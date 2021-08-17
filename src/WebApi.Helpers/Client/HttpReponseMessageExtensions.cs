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
                throw new InvalidOperationException("El código de estado de la respuesta es satisfactorio (200-299). No se puede realizar la operación ParseHttpResponseToNoSuccessResponseAsync.");

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

                    if (!(jsonContent.IndexOf(nameof(Server.MiddlewareExceptionModel.IsMiddlewareException), comparisonType: StringComparison.OrdinalIgnoreCase).Equals(-1)))
                    {
                        var deserializedContent = JsonSerializer.Deserialize<Server.MiddlewareExceptionModel>(jsonContent, WebApiBaseClient.WebApiClientParameters.SerializerOptions);

                        return new Bitai.WebApi.Client.NoSuccessResponseWithJsonExceptionContent(deserializedContent, statusCode, reasonPhrase, webServer, date);
                    }
                    else
                    {
                        return new Bitai.WebApi.Client.NoSuccessResponseWithJsonStringContent(jsonContent, Content_MediaType.ApplicationJson, statusCode, reasonPhrase, webServer, date);
                    }

                case MediaTypes.ApplicationProblemJson:
                    var problemJsonContent = await responseMessage.Content.ReadAsStringAsync();

                    return new Bitai.WebApi.Client.NoSuccessResponseWithJsonStringContent(problemJsonContent, Content_MediaType.ApplicationProblemJson, statusCode, reasonPhrase, webServer, date);

                case MediaTypes.TextHtml:
                    var htmlContent = await responseMessage.Content.ReadAsStringAsync();

                    return new Bitai.WebApi.Client.NoSuccessResponseWithHtmlContent(htmlContent, statusCode, reasonPhrase, webServer, date);

                default: //En caso de llegar a este caso, se debería implementar el manejo para el MIME desconocido. Por ahora se dispara un error informando el caso.
                    throw new NotSupportedException(string.Format("No se puede devolver una vista para las respuestas de error de solicitud http cuyo contenido en la respuesta sea el tipo \"{0}\". Se debe de implementar el soporte para ese tipo MIME en el Assembly: NetSqlAzMan.CustomBussinessLogic Clase: LdapWebApiClientHelpers.BaseHelpers Método: getHttpWebApiRequestException", mediaType));
            }
        }

        public static async Task<IHttpResponse> ToSuccessfulHttpResponseAsync<DTOType>(this HttpResponseMessage responseMessage)
        {
            //Validar StatusCode entre 200 y 299
            if (!(responseMessage.StatusCode >= System.Net.HttpStatusCode.OK && responseMessage.StatusCode < System.Net.HttpStatusCode.MultipleChoices))
                throw new InvalidOperationException("El código de estado de la respuesta no es satisfactorio. No se puede realizar la operación ParseHttpResponseToSuccessResponseWithDTOContentAsync.");

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
