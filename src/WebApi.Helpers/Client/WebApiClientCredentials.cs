using System;
using System.Collections.Generic;
using System.Text;

namespace Bitai.WebApi.Client
{
    /// <summary>
    /// Client credentials definition to get access to LDAP Web Api.
    /// </summary>
    public class WebApiClientCredentials
    {
        /// <summary>
        /// Identity Server (Authority) URL
        /// </summary>
        public string AuthorityUrl { get; set; }
        /// <summary>
        /// Required Api Scope by the client.
        /// </summary>
        public string ApiScope { get; set; }
        /// <summary>
        /// Client Id.
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// Client secret.
        /// </summary>
        public string ClientSecret { get; set; }
    }
}