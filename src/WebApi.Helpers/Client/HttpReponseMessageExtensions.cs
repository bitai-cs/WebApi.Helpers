using Bitai.WebApi.Common;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bitai.WebApi.Client
{
    public static class HttpReponseMessageExtensions
    {
        public static async Task<IHttpResponse> ToUnsuccessfulHttpResponseAsync(this HttpResponseMessage responseMessage)
        {
            //Validar StatusCode entre 200 y 299
            if (responseMessage.StatusCode >= System.Net.HttpStatusCode.OK && responseMessage.StatusCode < System.Net.HttpStatusCode.MultipleChoices)
                throw new InvalidOperationException($"The HTTP response message status code is {responseMessage.StatusCode} ({(int)responseMessage.StatusCode}). Cannot execute {nameof(HttpReponseMessageExtensions)}.{nameof(ToUnsuccessfulHttpResponseAsync)}.");

            var statusCode = responseMessage.StatusCode;
            var reasonPhrase = responseMessage.ReasonPhrase;
            var webServer = responseMessage.Headers.Server.ToArray().FirstOrDefault()?.Product.ToString();
            var date = responseMessage.Headers.Date.HasValue ? responseMessage.Headers.Date.Value.DateTime.ToString() : string.Empty;
            var mediaType = responseMessage.Content.Headers.ContentLength.Value.Equals(0) ? MediaTypes.NoContent : responseMessage.Content.Headers.ContentType.MediaType.ToLower();

            switch (mediaType)
            {
                case MediaTypes.NoContent:
                    return new Bitai.WebApi.Client.NoSuccessResponseWithEmptyContent(statusCode, reasonPhrase, webServer, date);

                case MediaTypes.ApplicationJson:
                    var jsonContent = await responseMessage.Content.ReadAsStringAsync();

                    return new Bitai.WebApi.Client.NoSuccessResponseWithJsonStringContent(jsonContent, Content_MediaType.ApplicationJson, statusCode, reasonPhrase, webServer, date);

                case MediaTypes.ApplicationProblemJson:
                    var problemJsonContent = await responseMessage.Content.ReadAsStringAsync();

                    if (!(problemJsonContent.IndexOf(nameof(Server.MiddlewareExceptionModel.IsMiddlewareException), comparisonType: StringComparison.OrdinalIgnoreCase).Equals(-1)))
                    {
                        var deserializedContent = JsonSerializer.Deserialize<Server.MiddlewareExceptionModel>(problemJsonContent, WebApiBaseClient.WebApiClientParameters.SerializerOptions);

                        return new Bitai.WebApi.Client.NoSuccessResponseWithJsonExceptionContent(deserializedContent, statusCode, reasonPhrase, webServer, date);
                    }
                    else
                    {
                        return new Bitai.WebApi.Client.NoSuccessResponseWithJsonStringContent(problemJsonContent, Content_MediaType.ApplicationProblemJson, statusCode, reasonPhrase, webServer, date);
                    }

                case MediaTypes.TextHtml:
                    var htmlContent = await responseMessage.Content.ReadAsStringAsync();

                    return new Bitai.WebApi.Client.NoSuccessResponseWithHtmlContent(htmlContent, statusCode, reasonPhrase, webServer, date);

                default: //In this case, handling of the required MIME type should be implemented. For now an error is triggered.
                    throw new NotSupportedException($"Unable to generate an {nameof(IHttpResponse)} for MIME \"{mediaType}\" type response content. Support must be implemented for the MIME type in {nameof(HttpReponseMessageExtensions)}.{nameof(ToUnsuccessfulHttpResponseAsync)}.");
            }
        }

        public static async Task<IHttpResponse> ToSuccessfulHttpResponseAsync<DTOType>(this HttpResponseMessage responseMessage)
        {
            //Validar StatusCode entre 200 y 299
            if (!(responseMessage.StatusCode >= System.Net.HttpStatusCode.OK && responseMessage.StatusCode < System.Net.HttpStatusCode.MultipleChoices))
                throw new InvalidOperationException($"The HTTP response message status code is {responseMessage.StatusCode} ({(int)responseMessage.StatusCode}). Cannot execute {nameof(HttpReponseMessageExtensions)}.{nameof(ToSuccessfulHttpResponseAsync)}.");

            var statusCode = responseMessage.StatusCode;
            var reasonPhrase = responseMessage.ReasonPhrase;
            var mediaType = responseMessage.Content.Headers.ContentLength.Value.Equals(0) ? MediaTypes.NoContent : responseMessage.Content.Headers.ContentType.MediaType.ToLower();
            var jsonContent = await responseMessage.Content.ReadAsStringAsync();
            var deserializedContent = await Task.Run(() => JsonSerializer.Deserialize<DTOType>(jsonContent, WebApiBaseClient.WebApiClientParameters.SerializerOptions));

            var successJsonContentResponse = new SuccessResponseWithJsonContent<DTOType>()
            {
                HttpStatusCode = statusCode,
                ReasonPhrase = reasonPhrase,
                WebServer = responseMessage.Headers.Server.ToArray()[0].Product.ToString(),
                Date = responseMessage.Headers.Date.HasValue ? responseMessage.Headers.Date.Value.LocalDateTime.ToString() : string.Empty,
                Content = deserializedContent
            };

            return successJsonContentResponse;
        }
    }
}
