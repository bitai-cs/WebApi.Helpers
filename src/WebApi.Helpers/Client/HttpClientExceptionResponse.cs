using System;

namespace Bitai.WebApi.Client
{
    public class HttpClientExceptionResponse : Exception
    {
        public HttpClientExceptionResponse(string message) : base(message) { }

        public HttpClientExceptionResponse(string message, Exception innerException) : base(message, innerException) { }
    }
}
