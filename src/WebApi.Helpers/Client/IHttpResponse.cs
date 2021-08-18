using System;
using System.Collections.Generic;
using System.Text;
using Bitai.WebApi.Common;

namespace Bitai.WebApi.Client
{
    /// <summary>
    /// Represents a response to an HTTP request sent to a Web Api.
    /// </summary>
    public interface IHttpResponse
    {
        /// <summary>
        /// True when the HTTP response is OK, or the HTTP code 
        /// is 200, otherwise False.
        /// </summary>
        bool IsSuccessResponse { get; }

        /// <summary>
        /// See <see cref="HttpStatusCode"/>.
        /// </summary>
        System.Net.HttpStatusCode HttpStatusCode { get; }
        
        /// <summary>
        /// Web server identifier; if it is available.
        /// </summary>
        string WebServer { get; }

        /// <summary>
        /// Web Server date; if it is available.
        /// </summary>
        string Date { get; }

        /// <summary>
        /// Http response reason phrase.
        /// </summary>
        string ReasonPhrase { get; }
        
        /// <summary>
        /// Type of response content.
        /// </summary>
        Content_MediaType ContentMediaType { get; }
    }

    /// <summary>
    /// Represents a response to an HTTP request sent to a Web Api.
    /// </summary>
    /// <typeparam name="TContent">Content type of the response. Por ejemplo string, DTO class</typeparam>
    public interface IHttpResponse<TContent> : IHttpResponse
    {
        /// <summary>
        /// Response content.
        /// </summary>
        TContent Content { get; }
    }
}
