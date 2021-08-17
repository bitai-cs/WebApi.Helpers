using IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Bitai.WebApi.Client
{
    /// <summary>
    /// This class wraps <see cref="HttpClient"/> and add some methods to 
    /// handle access tokens persistence and expiration.
    /// </summary>
    public class AuthorizedHttpClient : HttpClient
    {
        /// <summary>
        /// Constructor. See <see cref="HttpClient"./>
        /// </summary>
        public AuthorizedHttpClient() : base()
        {
        }
        /// <summary>
        /// Constructor. See <see cref="HttpClient"./>
        /// </summary>
        /// <param name="handler">See <see cref="HttpMessageHandler"/>.</param>
        public AuthorizedHttpClient(HttpMessageHandler handler) : base(handler)
        {
        }
        /// <summary>
        /// Constructor, See <see cref="HttpClient"./>
        /// </summary>
        /// <param name="handler">See <see cref="HttpMessageHandler"/>.</param>
        /// <param name="disposeHandler">true if the inner handler should be disposed of by HttpClient.Dispose; false if you intend to reuse the inner handler.</param>
        public AuthorizedHttpClient(HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler)
        {
        }



        #region Static members
        /// <summary>
        /// Dictionary of all generated access tokens
        /// </summary>
        private static readonly Dictionary<string, CachedAccessToken> cachedAccessTokens = new Dictionary<string, CachedAccessToken>();



        /// <summary>
        /// Check if a cached access token exists for a Web Api client and 
        /// if it has not expired yet; otherwise request a new access token 
        /// from the Web Api authority.
        /// </summary>
        /// <param name="webApiClientGuid">Unique indentifier of a Web Api Client (commonly inherited from <see cref="WebApiBaseClient"/>).</param>
        /// <param name="httpClient">See <see cref="AuthorizedHttpClient"/>.</param>
        /// <param name="clientCredentials">See <see cref="WebApiClientCredentials"/>.</param>
        /// <returns></returns>
        internal static async Task CheckClientCredentialsTokenHealth(string webApiClientGuid, AuthorizedHttpClient httpClient, WebApiClientCredentials clientCredentials)
        {
            TokenResponse cachedTokenResponse = null;
            DateTime? cachedTokenExpireDate;

            var now = DateTime.Now;

            if (!cachedAccessTokens.ContainsKey(webApiClientGuid))
            {
                cachedTokenResponse = await getClientCredentialsToken(httpClient, clientCredentials);

                cachedTokenExpireDate = getTokenExpireDate(cachedTokenResponse);

                cachedAccessTokens.Add(webApiClientGuid, new CachedAccessToken(cachedTokenResponse, cachedTokenExpireDate.Value));
            }
            else
            {
                var cachedAccessToken = cachedAccessTokens[webApiClientGuid];

                if (now >= cachedAccessToken.CachedTokenExpireDate)
                {
                    cachedTokenResponse = await getClientCredentialsToken(httpClient, clientCredentials);

                    cachedTokenExpireDate = getTokenExpireDate(cachedTokenResponse);

                    cachedAccessToken.CachedTokenResponse = cachedTokenResponse;
                    cachedAccessToken.CachedTokenExpireDate = cachedTokenExpireDate.Value;
                }
            }

            setAuthorizationHeaderWithBearerToken(httpClient, cachedTokenResponse);

            //Get an expire date for an Access Token
            DateTime getTokenExpireDate(TokenResponse tokenResponse)
            {
                return now.AddSeconds(cachedTokenResponse.ExpiresIn);
            }
        }



        private static async Task<TokenResponse> getClientCredentialsToken(AuthorizedHttpClient httpClient, WebApiClientCredentials clientCredentials)
        {
            var discoveryDocResponse = await httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = clientCredentials.AuthorityUrl,
                Policy = new DiscoveryPolicy
                {
                    RequireHttps = false
                }
            });

            if (discoveryDocResponse.IsError)
            {
                var exceptionMessage = $"Failed to get security token. An error occurred while getting the discovery document from authority {clientCredentials.AuthorityUrl}. Error: {discoveryDocResponse.Error} | Error type: {discoveryDocResponse.ErrorType}";

                if (discoveryDocResponse.HttpResponse != null)
                    exceptionMessage += $" Http Status Code: {Convert.ToInt32(discoveryDocResponse.HttpStatusCode)} | Http Message: {discoveryDocResponse.HttpErrorReason}";

                throw new Exception(exceptionMessage);
            }

            var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = discoveryDocResponse.TokenEndpoint,
                Scope = clientCredentials.ApiScope,
                ClientId = clientCredentials.ClientId,
                ClientSecret = clientCredentials.ClientSecret
            });

            if (tokenResponse.IsError)
                throw new Exception($"Can't get security token. Authority: {clientCredentials.AuthorityUrl} | ClientId: {clientCredentials.ClientId} | Error: {tokenResponse.Error}" + (string.IsNullOrEmpty(tokenResponse.ErrorDescription) ? string.Empty : $" | Error description: {tokenResponse.ErrorDescription}") + $" | Error type: {tokenResponse.ErrorType} | Http Message: {tokenResponse.HttpErrorReason}");

            return tokenResponse;
        }

        private static void setAuthorizationHeaderWithBearerToken(AuthorizedHttpClient httpClient, TokenResponse tokenResponse)
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
        }
        #endregion



        #region Inner Classes
        /// <summary>
        /// This class represents a cached access token and its expire date.
        /// </summary>
        private class CachedAccessToken
        {
            private TokenResponse _cachedTokenResponse;
            private DateTime _cachedTokenExpireDate;



            public TokenResponse CachedTokenResponse { get => _cachedTokenResponse; internal set => _cachedTokenResponse = value; }

            public DateTime CachedTokenExpireDate { get => _cachedTokenExpireDate; internal set => _cachedTokenExpireDate = value; }



            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="tokenResponse">Valid <see cref="TokenResponse"/>.</param>
            /// <param name="tokenExpireDate">Expire date of <paramref name="tokenResponse"/>.</param>
            internal CachedAccessToken(TokenResponse tokenResponse, DateTime tokenExpireDate)
            {
                _cachedTokenResponse = tokenResponse;
                _cachedTokenExpireDate = tokenExpireDate;
            }
        }
        #endregion  
    }
}
