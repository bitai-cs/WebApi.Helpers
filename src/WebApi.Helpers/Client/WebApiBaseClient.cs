using System;
using System.Collections.Concurrent;
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
        protected readonly HttpClientHandler Handler;
        protected readonly bool DisposeHandler;

        /// <summary>
        /// Dictionary of all generated access tokens
        /// </summary>
        private readonly ConcurrentDictionary<string, CachedAccessToken> _accessTokenCache = new ConcurrentDictionary<string, CachedAccessToken>();




        /// <summary>
        /// Security parameters to request an access token to the Identity Server.
        /// This access token will allow the client to make requests to the 
        /// LDAP Web Api; if it requires it.
        /// </summary>
        public WebApiClientCredential ClientCredential { get; set; }




        protected WebApiBaseClient(string webApiBaseUrl)
        {
            if (string.IsNullOrEmpty(webApiBaseUrl))
                throw new ArgumentNullException(nameof(webApiBaseUrl));

            if (webApiBaseUrl.EndsWith("/"))
                throw new ArgumentException("The base URL of the Web API cannot end with the character '/'");

            WebApiBaseUrl = webApiBaseUrl;
        }

        protected WebApiBaseClient(string webApiBaseUrl, HttpClientHandler handler, bool disposeHandler) : this(webApiBaseUrl)
        {
            Handler = handler;
            DisposeHandler = disposeHandler;
        }

        protected WebApiBaseClient(string webApiBaseUrl, WebApiClientCredential clientCredentials) : this(webApiBaseUrl)
        {
            ClientCredential = clientCredentials;
        }

        protected WebApiBaseClient(string webApiBaseUrl, WebApiClientCredential clientCredentials, HttpClientHandler handler, bool disposeHandler) : this(webApiBaseUrl, clientCredentials)
        {
            Handler = handler;
            DisposeHandler = disposeHandler;
        }




        public void ThrowClientRequestException(string exceptionMessage, IHttpResponse httpResponse)
        {
            throw GetClientRequestException(exceptionMessage, httpResponse);
        }

        public WebApiRequestException GetClientRequestException(string exceptionMessage, IHttpResponse httpResponse)
        {
            if (httpResponse.IsSuccessResponse)
                throw new InvalidOperationException($"The Web API response code is a success response code. Cannot initialize a {nameof(WebApiRequestException)} object with the specified {nameof(IHttpResponse)}.");

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
            if (setAuthorizationHeaderWithBearerToken && ClientCredential == null)
                throw new InvalidOperationException($"{nameof(setAuthorizationHeaderWithBearerToken)} was set to True, {nameof(ClientCredential)} cannot be Null.");

            if (WebApiClientParameters.ClientRequestTimeOut == 0)
                throw new InvalidOperationException("No se ha inicializado el parametro WebApiClientStartup.ClientRequestTimeOut");

            if (WebApiClientParameters.MaxResponseContentBufferSize == 0)
                throw new InvalidOperationException("No se ha inicializado el parametro WebApiClientStartup.MaxResponseContentBufferSize");

            AuthorizedHttpClient httpClient;
            if (Handler == null)
                httpClient = new AuthorizedHttpClient();
            else
                httpClient = new AuthorizedHttpClient(Handler, DisposeHandler);

            httpClient.Timeout = new TimeSpan(0, 0, WebApiClientParameters.ClientRequestTimeOut);
            httpClient.MaxResponseContentBufferSize = WebApiClientParameters.MaxResponseContentBufferSize;
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(MediaTypes.ApplicationJson));

            if (setAuthorizationHeaderWithBearerToken)
                await checkClientCredentialsTokenHealth(httpClient);

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




        /// <summary>
        /// Check if a cached access token exists for a Web Api client and 
        /// if it has not expired yet; otherwise request a new access token 
        /// from the Web Api authority.
        /// </summary>
        /// <param name="httpClient">See <see cref="AuthorizedHttpClient"/>.</param>
        /// <returns></returns>
        private async Task checkClientCredentialsTokenHealth(AuthorizedHttpClient httpClient)
        {
            TokenResponse tokenResponse = null;
            DateTime? tokenExpireDate;

            var now = DateTime.Now;
            var cacheKey = generateCacheKey();

            CachedAccessToken cachedTokenReponse;
            if (_accessTokenCache.TryGetValue(cacheKey, out cachedTokenReponse) && now <= cachedTokenReponse.TokenExpireDate)
            {
                tokenResponse = cachedTokenReponse.TokenResponse;
            }
            else
            {
                tokenResponse = await getClientCredentialsToken(httpClient);
                tokenExpireDate = getTokenExpireDate(tokenResponse);

                if (cachedTokenReponse == null)
                {
                    _accessTokenCache.TryAdd(cacheKey, new CachedAccessToken(tokenResponse, tokenExpireDate.Value));
                }
                else
                {
                    _accessTokenCache.TryUpdate(cacheKey, new CachedAccessToken(tokenResponse, tokenExpireDate.Value), cachedTokenReponse);
                }                
            }

            setAuthorizationHeaderWithBearerToken(httpClient, tokenResponse);

            string generateCacheKey()
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var input = $"{ClientCredential.AuthorityUrl.ToLower()}:{ClientCredential.ApiScope.ToLower()}:{ClientCredential.ClientId.ToLower()}:{ClientCredential.ClientSecret}";
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));

                return Convert.ToBase64String(hashBytes); // O usa un formato hexadecimal si prefieres
            }

            //Get an expire date for an Access Token
            DateTime getTokenExpireDate(TokenResponse tokenResponse)
            {
                return now.AddSeconds(tokenResponse.ExpiresIn - 10); //10 seconds for latency
            }
        }

        private async Task<TokenResponse> getClientCredentialsToken(AuthorizedHttpClient httpClient)
        {
            var discoveryDocResponse = await httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = ClientCredential.AuthorityUrl.ToLower(),
                Policy = new DiscoveryPolicy
                {
                    RequireHttps = false
                }
            });

            if (discoveryDocResponse.IsError)
            {
                var exceptionMessage = $"Failed to get security token. An error occurred while getting the discovery document from authority {ClientCredential.AuthorityUrl}. Error: {discoveryDocResponse.Error} | Error type: {discoveryDocResponse.ErrorType}";

                if (discoveryDocResponse.HttpResponse != null)
                    exceptionMessage += $" Http Status Code: {Convert.ToInt32(discoveryDocResponse.HttpStatusCode)} | Http Message: {discoveryDocResponse.HttpErrorReason}";

                throw new Exception(exceptionMessage);
            }

            var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = discoveryDocResponse.TokenEndpoint,
                Scope = ClientCredential.ApiScope,
                ClientId = ClientCredential.ClientId,
                ClientSecret = ClientCredential.ClientSecret
            });

            if (tokenResponse.IsError)
                throw new Exception($"Can't get security token. Authority: {ClientCredential.AuthorityUrl} | ClientId: {ClientCredential.ClientId} | Error: {tokenResponse.Error}" + (string.IsNullOrEmpty(tokenResponse.ErrorDescription) ? string.Empty : $" | Error description: {tokenResponse.ErrorDescription}") + $" | Error type: {tokenResponse.ErrorType} | Http Message: {tokenResponse.HttpErrorReason}");

            return tokenResponse;
        }

        private void setAuthorizationHeaderWithBearerToken(AuthorizedHttpClient httpClient, TokenResponse tokenResponse)
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
        }




        #region Inner Classes
        /// <summary>
        /// This class represents a cached access token and its expire date.
        /// </summary>
        private class CachedAccessToken
        {
            private TokenResponse _tokenResponse;
            private DateTime _tokenExpireDate;



            public TokenResponse TokenResponse { get => _tokenResponse; internal set => _tokenResponse = value; }

            public DateTime TokenExpireDate { get => _tokenExpireDate; internal set => _tokenExpireDate = value; }



            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="tokenResponse">Valid <see cref="IdentityModel.Client.TokenResponse"/>.</param>
            /// <param name="tokenExpireDate">Expire date of <paramref name="tokenResponse"/>.</param>
            internal CachedAccessToken(TokenResponse tokenResponse, DateTime tokenExpireDate)
            {
                _tokenResponse = tokenResponse;
                _tokenExpireDate = tokenExpireDate;
            }
        }
        #endregion  

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
