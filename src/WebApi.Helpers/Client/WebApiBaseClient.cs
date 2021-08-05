using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bitai.WebApi.Common;

namespace Bitai.WebApi.Client
{

    public abstract partial class WebApiBaseClient<DTOType>
    {
        protected readonly string WebApiBaseUrl;



        protected WebApiBaseClient(string webApiBaseUrl)
        {
            if (string.IsNullOrEmpty(webApiBaseUrl))
                throw new ArgumentNullException(nameof(webApiBaseUrl));

            if (webApiBaseUrl.EndsWith("/"))
                throw new ArgumentException("The base URL of the Web API cannot end with the character '/'");

            WebApiBaseUrl = webApiBaseUrl;
        }



        public bool IsNoSuccessResponse(IHttpResponse httpResponse)
        {
            return !httpResponse.IsSuccessResponse;
        }

        public void ThrowClientRequestException(string exceptionMessage, IHttpResponse httpResponse)
        {
            throw GetClientRequestException(exceptionMessage, httpResponse);
        }

        public Exception GetClientRequestException(string exceptionMessage, IHttpResponse httpResponse)
        {
            if (httpResponse.IsSuccessResponse)
                throw new InvalidOperationException("La respuesta del Web Api es satisfactoria. No se puede inferir un objeto Exception de IHttpResponse.");

            return new WebApiRequestException(exceptionMessage, httpResponse);
        }

        public DTOType GetDTOFromResponse(IHttpResponse httpResponse)
        {
            return ((SuccessResponseWithJsonContent<DTOType>)httpResponse).Content;
        }

        public Task<DTOType> GetDTOFromResponseAsync(IHttpResponse httpResponse)
        {
            return Task.Run(() => GetDTOFromResponse(httpResponse));
        }

        public IEnumerable<DTOType> GetEnumerableDTOFromResponse(IHttpResponse httpResponse)
        {
            return ((SuccessResponseWithJsonContent<IEnumerable<DTOType>>)httpResponse).Content;
        }

        public Task<IEnumerable<DTOType>> GetEnumerableDTOFromResponseAsync(IHttpResponse httpResponse)
        {
            return Task.Run(() => GetEnumerableDTOFromResponse(httpResponse));
        }



        protected HttpClient CreateHttpClient(bool clearDefaultRequestHeaders = true, Header_AcceptType headerAcceptType = Header_AcceptType.ApplicationJson)
        {
            if (WebApiClientParameters.ClientRequestTimeOut == 0)
                throw new InvalidOperationException("No se ha inicializado el parametro WebApiClientStartup.ClientRequestTimeOut");

            if (WebApiClientParameters.MaxResponseContentBufferSize == 0)
                throw new InvalidOperationException("No se ha inicializado el parametro WebApiClientStartup.MaxResponseContentBufferSize");

            //#if DEBUG
            //            //For development and debugging purposes only
            //            if (System.Net.ServicePointManager.ServerCertificateValidationCallback == null)
            //            {
            //                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
            //                {
            //                    return true;
            //                };
            //            }
            //#endif

            var httpClient = new HttpClient
            {
                Timeout = new TimeSpan(0, 0, WebApiClientParameters.ClientRequestTimeOut),
                MaxResponseContentBufferSize = WebApiClientParameters.MaxResponseContentBufferSize
            };

            if (clearDefaultRequestHeaders)
                httpClient.DefaultRequestHeaders.Clear();

            switch (headerAcceptType)
            {
                case Header_AcceptType.ApplicationJson:
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(MediaTypes.ApplicationJson));
                    break;
                default:
                    throw new Exception("No se ha especificado el tipo de Header Accept.");
            }

            return httpClient;
        }

        protected async Task<IHttpResponse> ParseHttpResponseToNoSuccessResponseAsync(HttpResponseMessage responseMessage)
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
                        var deserializedContent = JsonSerializer.Deserialize<Server.MiddlewareExceptionModel>(jsonContent, WebApiClientParameters.SerializerOptions);

                        return new Bitai.WebApi.Client.NoSuccessResponseWithJsonExceptionContent(deserializedContent, statusCode, reasonPhrase, webServer, date);
                    }
                    else
                    {
                        return new Bitai.WebApi.Client.NoSuccessResponseWithJsonStringContent(jsonContent, Conten_MediaType.ApplicationJson, statusCode, reasonPhrase, webServer, date);
                    }

                case MediaTypes.ApplicationProblemJson:
                    var problemJsonContent = await responseMessage.Content.ReadAsStringAsync();

                    return new Bitai.WebApi.Client.NoSuccessResponseWithJsonStringContent(problemJsonContent, Conten_MediaType.ApplicationProblemJson, statusCode, reasonPhrase, webServer, date);

                case MediaTypes.TextHtml:
                    var htmlContent = await responseMessage.Content.ReadAsStringAsync();

                    return new Bitai.WebApi.Client.NoSuccessResponseWithHtmlContent(htmlContent, statusCode, reasonPhrase, webServer, date);

                default: //En caso de llegar a este caso, se debería implementar el manejo para el MIME desconocido. Por ahora se dispara un error informando el caso.
                    throw new NotSupportedException(string.Format("No se puede devolver una vista para las respuestas de error de solicitud http cuyo contenido en la respuesta sea el tipo \"{0}\". Se debe de implementar el soporte para ese tipo MIME en el Assembly: NetSqlAzMan.CustomBussinessLogic Clase: LdapWebApiClientHelpers.BaseHelpers Método: getHttpWebApiRequestException", mediaType));
            }
        }

        protected async Task<IHttpResponse> ParseHttpResponseToSuccessDTOResponseAsync(HttpResponseMessage responseMessage)
        {
            //Validar StatusCode entre 200 y 299
            if (!(responseMessage.StatusCode >= System.Net.HttpStatusCode.OK && responseMessage.StatusCode < System.Net.HttpStatusCode.MultipleChoices))
                throw new InvalidOperationException("El código de estado de la respuesta no es satisfactorio. No se puede realizar la operación ParseHttpResponseToSuccessResponseWithDTOContentAsync.");

            var statusCode = responseMessage.StatusCode;
            var reasonPhrase = responseMessage.ReasonPhrase;
            var mediaType = responseMessage.Content.Headers.ContentLength.Value.Equals(0) ? MediaTypes.NoContent : responseMessage.Content.Headers.ContentType.MediaType.ToLower();
            var jsonContent = await responseMessage.Content.ReadAsStringAsync();
            var deserializedContent = await Task.Run(() => JsonSerializer.Deserialize<DTOType>(jsonContent, WebApiClientParameters.SerializerOptions));

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

        protected async Task<IHttpResponse> ParseHttpResponseToSuccessEnumerableDTOResponseAsync(HttpResponseMessage responseMessage)
        {
            //Validar StatusCode entre 200 y 299
            if (!(responseMessage.StatusCode >= System.Net.HttpStatusCode.OK && responseMessage.StatusCode < System.Net.HttpStatusCode.MultipleChoices))
                throw new InvalidOperationException("El código de estado de la respuesta no es satisfactorio. No se puede realizar la operación ParseHttpResponseToSuccessResponseWithDTOContentAsync.");

            var statusCode = responseMessage.StatusCode;
            var reasonPhrase = responseMessage.ReasonPhrase;
            var mediaType = responseMessage.Content.Headers.ContentLength.Value.Equals(0) ? MediaTypes.NoContent : responseMessage.Content.Headers.ContentType.MediaType.ToLower();
            var jsonContent = await responseMessage.Content.ReadAsStringAsync();
            var deserializedContent = await Task.Run(() => JsonSerializer.Deserialize<IEnumerable<DTOType>>(jsonContent, WebApiClientParameters.SerializerOptions));

            var successJsonContentResponse = new SuccessResponseWithJsonContent<IEnumerable<DTOType>>()
            {
                HttpStatusCode = statusCode,
                ReasonPhrase = reasonPhrase,
                WebServer = responseMessage.Headers.Server.ToArray()[0].Product.ToString(),
                Date = responseMessage.Headers.Date.HasValue ? responseMessage.Headers.Date.Value.LocalDateTime.ToString() : "?",
                Content = deserializedContent
            };

            return successJsonContentResponse;
        }

        protected StringContent GetStringContentFromObject(object dto, Content_Encoding contentEncoding = Content_Encoding.UTF8, Conten_MediaType contentMediaType = Conten_MediaType.ApplicationJson)
        {
            Encoding encoding;
            switch (contentEncoding)
            {
                case Content_Encoding.UTF8:
                    encoding = Encoding.UTF8;
                    break;
                default:
                    throw new NotImplementedException($"encoding: {contentEncoding} not implemented.");
            }

            string mediaType;
            switch (contentMediaType)
            {
                case Conten_MediaType.ApplicationJson:
                    mediaType = MediaTypes.ApplicationJson;
                    break;
                default:
                    throw new NotImplementedException($"mediatype: {contentMediaType} not implemented.");
            }

            return new StringContent(JsonSerializer.Serialize(dto, WebApiClientParameters.SerializerOptions), encoding, mediaType);
        }
    }
}
