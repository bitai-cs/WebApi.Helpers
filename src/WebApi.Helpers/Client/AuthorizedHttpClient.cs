using IdentityModel.Client;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
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
    }
}
