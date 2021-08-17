using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bitai.WebApi.Common;
using IdentityModel.Client;

namespace Bitai.WebApi.Client
{
    public abstract class WebApiBaseClient
    {
        protected readonly string WebApiBaseUrl;
        protected readonly string WebApiClientGuid = Guid.NewGuid().ToString();




        /// <summary>
        /// Security parameters to request an access token to the Identity Server.
        /// This access token will allow the client to make requests to the 
        /// LDAP Web Api; if it requires it.
        /// </summary>
        public WebApiClientCredentials ClientCredentials { get; set; }




        protected WebApiBaseClient(string webApiBaseUrl)
        {
            if (string.IsNullOrEmpty(webApiBaseUrl))
                throw new ArgumentNullException(nameof(webApiBaseUrl));

            if (webApiBaseUrl.EndsWith("/"))
                throw new ArgumentException("The base URL of the Web API cannot end with the character '/'");

            WebApiBaseUrl = webApiBaseUrl;
        }

        protected WebApiBaseClient(string webApiBaseUrl, WebApiClientCredentials clientCredentials) : this(webApiBaseUrl)
        {
            ClientCredentials = clientCredentials;
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

        public DTOType GetDTOFromResponse<DTOType>(IHttpResponse httpResponse)
        {
            return ((SuccessResponseWithJsonContent<DTOType>)httpResponse).Content;
        }

        public Task<DTOType> GetDTOFromResponseAsync<DTOType>(IHttpResponse httpResponse)
        {
            return Task.Run(() => GetDTOFromResponse<DTOType>(httpResponse));
        }

        public IEnumerable<DTOType> GetEnumerableDTOFromResponse<DTOType>(IHttpResponse httpResponse)
        {
            return ((SuccessResponseWithJsonContent<IEnumerable<DTOType>>)httpResponse).Content;
        }

        public Task<IEnumerable<DTOType>> GetEnumerableDTOFromResponseAsync<DTOType>(IHttpResponse httpResponse)
        {
            return Task.Run(() => GetEnumerableDTOFromResponse<DTOType>(httpResponse));
        }




        protected virtual async Task<AuthorizedHttpClient> CreateHttpClient(bool setAuthorizationHeaderWithBearerToken = false)
        {
            if (setAuthorizationHeaderWithBearerToken && ClientCredentials == null)
                throw new InvalidOperationException($"{nameof(setAuthorizationHeaderWithBearerToken)} was set to True, {nameof(ClientCredentials)} cannot be Null.");

            if (WebApiClientParameters.ClientRequestTimeOut == 0)
                throw new InvalidOperationException("No se ha inicializado el parametro WebApiClientStartup.ClientRequestTimeOut");

            if (WebApiClientParameters.MaxResponseContentBufferSize == 0)
                throw new InvalidOperationException("No se ha inicializado el parametro WebApiClientStartup.MaxResponseContentBufferSize");

            var httpClient = new AuthorizedHttpClient
            {
                Timeout = new TimeSpan(0, 0, WebApiClientParameters.ClientRequestTimeOut),
                MaxResponseContentBufferSize = WebApiClientParameters.MaxResponseContentBufferSize
            };

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(MediaTypes.ApplicationJson));

            if (setAuthorizationHeaderWithBearerToken)
                await AuthorizedHttpClient.CheckClientCredentialsTokenHealth(WebApiClientGuid, httpClient, ClientCredentials);

            return httpClient;
        }

        protected StringContent GetStringContentFromObject(object dto, Content_Encoding contentEncoding = Content_Encoding.UTF8, Content_MediaType contentMediaType = Content_MediaType.ApplicationJson)
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
                case Content_MediaType.ApplicationJson:
                    mediaType = MediaTypes.ApplicationJson;
                    break;
                default:
                    throw new NotImplementedException($"mediatype: {contentMediaType} not implemented.");
            }

            return new StringContent(JsonSerializer.Serialize(dto, WebApiClientParameters.SerializerOptions), encoding, mediaType);
        }




        #region Static classes
        public static class WebApiClientParameters
        {
            static public int ClientRequestTimeOut { get; set; }

            static public long MaxResponseContentBufferSize { get; set; }

            static public JsonSerializerOptions SerializerOptions { get; set; }


            /// <summary>
            /// Constructor Static que inicializa los parametros con valores por defecto.
            /// </summary>
            static WebApiClientParameters()
            {
                ClientRequestTimeOut = 3 * 60;

                MaxResponseContentBufferSize = 1 * 1024 * 1024;

                SerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            }
        }
        #endregion
    }
}
