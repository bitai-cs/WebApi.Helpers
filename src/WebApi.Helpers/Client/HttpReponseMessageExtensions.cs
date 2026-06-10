using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Bitai.WebApi.Common;

namespace Bitai.WebApi.Client
{
    public static class HttpReponseMessageExtensions
    {
        public static async Task<IHttpResponse> ToUnsuccessfulHttpResponseAsync(this HttpResponseMessage responseMessage)
        {
            if (responseMessage.IsSuccessStatusCode)
                throw new HttpClientExceptionResponse($"The HTTP response message status code is {responseMessage.StatusCode} ({(int)responseMessage.StatusCode}). Cannot execute {nameof(HttpReponseMessageExtensions)}.{nameof(ToUnsuccessfulHttpResponseAsync)}.");

            var statusCode = responseMessage.StatusCode;
            var reasonPhrase = responseMessage.ReasonPhrase;
            var webServer = ParseWebServerHeader(responseMessage);
            var date = responseMessage.Headers.Date?.UtcDateTime.ToString("O") ?? string.Empty;
            var mediaType = responseMessage.Content?.Headers?.ContentType?.MediaType?.ToLower() ?? MediaTypes.NoContent;

            switch (mediaType)
            {
                case MediaTypes.NoContent:
                    return new Bitai.WebApi.Client.NoSuccessResponseWithEmptyContent(statusCode, reasonPhrase, webServer, date);

                case MediaTypes.ApplicationJson:
                    var jsonContent = await responseMessage.Content.ReadAsStringAsync();

                    return new Bitai.WebApi.Client.NoSuccessResponseWithJsonStringContent(jsonContent, Content_MediaType.ApplicationJson, statusCode, reasonPhrase, webServer, date);

                case MediaTypes.ApplicationProblemJson:
                    var problemJsonContent = await responseMessage.Content.ReadAsStringAsync();

                    // BITAI: Legacy code.
                    //if (!(problemJsonContent.IndexOf(nameof(Server.MiddlewareExceptionModel.IsMiddlewareException), comparisonType: StringComparison.OrdinalIgnoreCase).Equals(-1)))

                    using (var jsonDoc = JsonDocument.Parse(problemJsonContent))
                    {
                        // BITAI: This is a workaround to determine if the problem JSON content is a MiddlewareExceptionModel or not, since the media type is "application/problem+json" for both cases. If the content contains the "IsMiddlewareException" property, it is deserialized as a MiddlewareExceptionModel. Otherwise, it is treated as a generic problem JSON content and returned as a string.
                        if (jsonDoc.RootElement.TryGetPropertyCaseInsensitive(nameof(Server.MiddlewareExceptionModel.IsMiddlewareException), out var isMiddlewareExceptionProperty))
                        {
                            var deserializedContent = JsonSerializer.Deserialize<Server.MiddlewareExceptionModel>(problemJsonContent, WebApiBaseClient.WebApiClientParameters.SerializerOptions);

                            return new Bitai.WebApi.Client.NoSuccessResponseWithJsonExceptionContent(deserializedContent, statusCode, reasonPhrase, webServer, date);
                        }
                        else
                        {
                            return new Bitai.WebApi.Client.NoSuccessResponseWithJsonStringContent(problemJsonContent, Content_MediaType.ApplicationProblemJson, statusCode, reasonPhrase, webServer, date);
                        }
                    }

                case MediaTypes.TextHtml:
                    var htmlContent = await responseMessage.Content.ReadAsStringAsync();

                    return new Bitai.WebApi.Client.NoSuccessResponseWithHtmlContent(htmlContent, statusCode, reasonPhrase, webServer, date);

                default: //In this case, handling of the required MIME type should be implemented. For now an error is triggered.
                    throw new HttpClientExceptionResponse($"Unable to generate an {nameof(IHttpResponse)} for MIME \"{mediaType}\" type response content. Support must be implemented for the MIME type in {nameof(HttpReponseMessageExtensions)}.{nameof(ToUnsuccessfulHttpResponseAsync)}.");
            }
        }

        public static async Task<IHttpResponse> ToSuccessfulHttpResponseAsync<DTOType>(this HttpResponseMessage responseMessage)
        {
            if (!responseMessage.IsSuccessStatusCode)
                throw new HttpClientExceptionResponse($"The HTTP response message status code is {responseMessage.StatusCode} ({(int)responseMessage.StatusCode}). Cannot execute {nameof(HttpReponseMessageExtensions)}.{nameof(ToSuccessfulHttpResponseAsync)}.");

            var mediaType = responseMessage.Content?.Headers?.ContentType?.MediaType?.ToLower() ?? MediaTypes.NoContent;
            if (mediaType != MediaTypes.ApplicationJson && mediaType != MediaTypes.ApplicationProblemJson)
                throw new HttpClientExceptionResponse($"Successful response with unsupported media type \"{mediaType}\".");

            var statusCode = responseMessage.StatusCode;
            var reasonPhrase = responseMessage.ReasonPhrase;

            // Handling content: if the content is empty, the content is set to default and the media type is set to "NoContent". If the content is not empty, it is deserialized according to the media type. For now, only JSON content is supported. If the media type is not supported, an error is triggered.
            var content = responseMessage.Content;
            var jsonContent = content is null ? string.Empty : await responseMessage.Content.ReadAsStringAsync();
            DTOType? deserializedContent = default;
            if (!string.IsNullOrWhiteSpace(jsonContent))
            {
                deserializedContent = JsonSerializer.Deserialize<DTOType>(jsonContent, WebApiBaseClient.WebApiClientParameters.SerializerOptions);
            }

            var successJsonContentResponse = new SuccessResponseWithJsonContent<DTOType>()
            {
                HttpStatusCode = statusCode,
                ReasonPhrase = reasonPhrase,
                WebServer = ParseWebServerHeader(responseMessage),
                Date = responseMessage.Headers.Date?.UtcDateTime.ToString("O") ?? string.Empty,
                Content = deserializedContent
            };

            return successJsonContentResponse;
        }


        #region Private methods
        private static string ParseWebServerHeader(HttpResponseMessage responseMessage)
        {
            var serverValues = responseMessage.Headers.Server;

            if (serverValues == null || !serverValues.Any())
                return "(Server info not provided)";

            var parts = serverValues
                .Select(p =>
                {
                    var product = p.Product?.ToString();
                    var comment = p.Comment?.ToString();

                    if (!string.IsNullOrWhiteSpace(product) && !string.IsNullOrWhiteSpace(comment))
                        return $"{product} {comment}";

                    if (!string.IsNullOrWhiteSpace(product))
                        return product;

                    if (!string.IsNullOrWhiteSpace(comment))
                        return comment;

                    return string.Empty;
                })
                .Where(s => !string.IsNullOrWhiteSpace(s));

            return parts.Any()
                ? string.Join(" ", parts)
                : "(Server info not provided)";
        }

        private static bool TryGetPropertyCaseInsensitive(this JsonElement element, string propertyName, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }
        #endregion
    }
}
