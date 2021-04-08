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
            var _s = (SuccessResponseWithJsonContent<DTOType>)httpResponse;
            return _s.Content;
        }

        public Task<DTOType> GetDTOFromResponseAsync(IHttpResponse httpResponse)
        {
            return Task.Run(() => GetDTOFromResponse(httpResponse));
        }

        public IEnumerable<DTOType> GetEnumerableDTOFromResponse(IHttpResponse httpResponse)
        {
            var _s = (SuccessResponseWithJsonContent<IEnumerable<DTOType>>)httpResponse;
            return _s.Content;
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

#if DEBUG
            if (System.Net.ServicePointManager.ServerCertificateValidationCallback == null)
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
                {
                    return true;
                };
            }
#endif
            var _client = new HttpClient
            {
                BaseAddress = new Uri(this.WebApiBaseUrl),
                Timeout = new TimeSpan(0, 0, WebApiClientParameters.ClientRequestTimeOut),
                MaxResponseContentBufferSize = WebApiClientParameters.MaxResponseContentBufferSize
            };

            if (clearDefaultRequestHeaders)
                _client.DefaultRequestHeaders.Clear();

            switch (headerAcceptType)
            {
                case Header_AcceptType.ApplicationJson:
                    _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(MediaTypes.ApplicationJson));
                    break;
                default:
                    throw new Exception("No se ha especificado el tipo de Header Accept.");
            }

            return _client;
        }

        protected async Task<IHttpResponse> ParseHttpResponseToNoSuccessResponseAsync(HttpResponseMessage responseMessage)
        {
            //Validar StatusCode entre 200 y 299
            if (responseMessage.StatusCode >= System.Net.HttpStatusCode.OK && responseMessage.StatusCode < System.Net.HttpStatusCode.MultipleChoices)
                throw new InvalidOperationException("El código de estado de la respuesta es satisfactorio (200-299). No se puede realizar la operación ParseHttpResponseToNoSuccessResponseAsync.");

            var _statusCode = responseMessage.StatusCode;
            var _reasonPhrase = responseMessage.ReasonPhrase;
            var _webServer = responseMessage.Headers.Server.ToArray()[0].Product.ToString();
            var _date = responseMessage.Headers.Date.HasValue ? responseMessage.Headers.Date.Value.LocalDateTime.ToString() : "?";

            string _mediaType;
            if (responseMessage.Content.Headers.ContentLength.Value.Equals(0))
                _mediaType = MediaTypes.NoContent;
            else
                _mediaType = responseMessage.Content.Headers.ContentType.MediaType.ToLower();

            switch (_mediaType)
            {
                case MediaTypes.NoContent:
                    return new Bitai.WebApi.Client.NoSuccessResponseWithEmptyContent(_statusCode, _reasonPhrase, _webServer, _date);

                case MediaTypes.ApplicationJson:
                    var _jsonContent = await responseMessage.Content.ReadAsStringAsync();

                    if (!(_jsonContent.IndexOf("IsExceptionJsonFormat", comparisonType: StringComparison.OrdinalIgnoreCase).Equals(-1)))
                    {
                        var _deserializedContent = JsonSerializer.Deserialize<Server.MiddlewareException>(_jsonContent, WebApiClientParameters.SerializerOptions);

                        return new Bitai.WebApi.Client.NoSuccessResponseWithJsonExceptionContent(_deserializedContent, _statusCode, _reasonPhrase, _webServer, _date);
                    }
                    else
                    {
                        return new Bitai.WebApi.Client.NoSuccessResponseWithJsonStringContent(_jsonContent, Conten_MediaType.ApplicationJson, _statusCode, _reasonPhrase, _webServer, _date);
                    }

                case MediaTypes.ApplicationProblemJson:
                    var _problemJsonContent = await responseMessage.Content.ReadAsStringAsync();

                    return new Bitai.WebApi.Client.NoSuccessResponseWithJsonStringContent(_problemJsonContent, Conten_MediaType.ApplicationProblemJson, _statusCode, _reasonPhrase, _webServer, _date);

                case MediaTypes.TextHtml:
                    var _htmlContent = await responseMessage.Content.ReadAsStringAsync();

                    return new Bitai.WebApi.Client.NoSuccessResponseWuthHtmlContent(_htmlContent, _statusCode, _reasonPhrase, _webServer, _date);

                default: //En caso de llegar a este caso, se debería implementar el manejo para el MIME desconocido. Por ahora se dispara un error informando el caso.
                    throw new NotSupportedException(string.Format("No se puede devolver una vista para las respuestas de error de solicitud http cuyo contenido en la respuesta sea el tipo \"{0}\". Se debe de implementar el soporte para ese tipo MIME en el Assembly: NetSqlAzMan.CustomBussinessLogic Clase: LdapWebApiClientHelpers.BaseHelpers Método: getHttpWebApiRequestException", _mediaType));
            }
        }

        protected async Task<IHttpResponse> ParseHttpResponseToSuccessDTOResponseAsync(HttpResponseMessage responseMessage)
        {
            //Validar StatusCode entre 200 y 299
            if (!(responseMessage.StatusCode >= System.Net.HttpStatusCode.OK && responseMessage.StatusCode < System.Net.HttpStatusCode.MultipleChoices))
                throw new InvalidOperationException("El código de estado de la respuesta no es satisfactorio. No se puede realizar la operación ParseHttpResponseToSuccessResponseWithDTOContentAsync.");

            var _statusCode = responseMessage.StatusCode;

            var _reasonPhrase = responseMessage.ReasonPhrase;

            string _mediaType;
            if (responseMessage.Content.Headers.ContentLength.Value.Equals(0))
                _mediaType = MediaTypes.NoContent;
            else
                _mediaType = responseMessage.Content.Headers.ContentType.MediaType.ToLower();

            string _jsonContent = await responseMessage.Content.ReadAsStringAsync();

            var _deserializedContent = await Task.Run(() => JsonSerializer.Deserialize<DTOType>(_jsonContent, WebApiClientParameters.SerializerOptions));

            var _successJsonContentResponse = new SuccessResponseWithJsonContent<DTOType>()
            {
                HttpStatusCode = _statusCode,
                ReasonPhrase = _reasonPhrase,
                WebServer = responseMessage.Headers.Server.ToArray()[0].Product.ToString(),
                Date = responseMessage.Headers.Date.HasValue ? responseMessage.Headers.Date.Value.LocalDateTime.ToString() : "?",
                Content = _deserializedContent
            };

            return _successJsonContentResponse;
        }

        protected async Task<IHttpResponse> ParseHttpResponseToSuccessEnumerableDTOResponseAsync(HttpResponseMessage responseMessage)
        {
            //Validar StatusCode entre 200 y 299
            if (!(responseMessage.StatusCode >= System.Net.HttpStatusCode.OK && responseMessage.StatusCode < System.Net.HttpStatusCode.MultipleChoices))
                throw new InvalidOperationException("El código de estado de la respuesta no es satisfactorio. No se puede realizar la operación ParseHttpResponseToSuccessResponseWithDTOContentAsync.");

            var _statusCode = responseMessage.StatusCode;

            var _reasonPhrase = responseMessage.ReasonPhrase;

            string _mediaType;
            if (responseMessage.Content.Headers.ContentLength.Value.Equals(0))
                _mediaType = MediaTypes.NoContent;
            else
                _mediaType = responseMessage.Content.Headers.ContentType.MediaType.ToLower();

            string _jsonContent = await responseMessage.Content.ReadAsStringAsync();

            var _deserializedContent = await Task.Run(() => JsonSerializer.Deserialize<IEnumerable<DTOType>>(_jsonContent, WebApiClientParameters.SerializerOptions));

            var _successJsonContentResponse = new SuccessResponseWithJsonContent<IEnumerable<DTOType>>()
            {
                HttpStatusCode = _statusCode,
                ReasonPhrase = _reasonPhrase,
                WebServer = responseMessage.Headers.Server.ToArray()[0].Product.ToString(),
                Date = responseMessage.Headers.Date.HasValue ? responseMessage.Headers.Date.Value.LocalDateTime.ToString() : "?",
                Content = _deserializedContent
            };

            return _successJsonContentResponse;
        }

        protected StringContent GetStringContentFromObject(object dto, Content_Encoding encoding = Content_Encoding.UTF8, Conten_MediaType mediaType = Conten_MediaType.ApplicationJson)
        {
            Encoding _encoding;
            switch (encoding)
            {
                case Content_Encoding.UTF8:
                    _encoding = Encoding.UTF8;
                    break;
                default:
                    throw new NotImplementedException($"encoding: {encoding} not implemented.");
            }

            string _mediaType;
            switch (mediaType)
            {
                case Conten_MediaType.ApplicationJson:
                    _mediaType = MediaTypes.ApplicationJson;
                    break;
                default:
                    throw new NotImplementedException($"mediatype: {mediaType} not implemented.");
            }

            return new StringContent(JsonSerializer.Serialize(dto, WebApiClientParameters.SerializerOptions), _encoding, _mediaType);
        }
    }
}
