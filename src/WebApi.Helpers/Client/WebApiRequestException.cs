using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Bitai.WebApi.Client
{
    public class WebApiRequestException : Exception
    {
        public WebApiRequestException(string message, IHttpResponse relatedHttpResponse) : base(message)
        {
            NoSuccessResponse = relatedHttpResponse;
        }

        public IHttpResponse NoSuccessResponse { get; }
    }
}