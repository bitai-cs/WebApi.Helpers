using System;
using System.Collections.Generic;
using System.Text;
using Bitai.WebApi.Common;

namespace Bitai.WebApi.Client
{
    public interface IHttpResponse
    {
        bool IsSuccessResponse { get; }
        System.Net.HttpStatusCode HttpStatusCode { get; }
        string WebServer { get; }
        string Date { get; }
        string ReasonPhrase { get; }
        Conten_MediaType ContentType { get; }
    }

    public interface IHttpResponse<TContent> : IHttpResponse
    {

        TContent Content { get; }
    }
}
