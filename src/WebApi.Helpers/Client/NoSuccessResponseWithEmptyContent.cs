using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Bitai.WebApi.Common;

namespace Bitai.WebApi.Client
{
    public class NoSuccessResponseWithEmptyContent : IHttpResponse<string>
    {
        public NoSuccessResponseWithEmptyContent(HttpStatusCode httpStatusCode, string reasonPhrase, string webServer, string date)
        {
            this.HttpStatusCode = httpStatusCode;
            this.ReasonPhrase = reasonPhrase;
            this.WebServer = webServer;
            this.Date = date;
        }



        public HttpStatusCode HttpStatusCode { get; set; }

        public string WebServer { get; set; }

        public string Date { get; set; }

        public string Content => string.Empty;

        public string ReasonPhrase { get; set; }

        public bool IsSuccessResponse => false;

        public Content_MediaType ContentMediaType => Content_MediaType.NoContent;
    }
}