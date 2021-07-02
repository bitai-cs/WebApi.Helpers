﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Bitai.WebApi.Common;

namespace Bitai.WebApi.Client
{
    public class NoSuccessResponseWithJsonExceptionContent : IHttpResponse<Server.MiddlewareException>
    {
        public NoSuccessResponseWithJsonExceptionContent(Server.MiddlewareException exception, HttpStatusCode httpStatusCode, string reasonPhrase, string webServer, string date)
        {
            this.Content = exception;
            this.HttpStatusCode = httpStatusCode;
            this.ReasonPhrase = reasonPhrase;
            this.WebServer = webServer;
            this.Date = date;
        }



        public HttpStatusCode HttpStatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public string WebServer { get; set; }

        public string Date { get; set; }

        public Server.MiddlewareException Content { get; set; }

        public bool IsSuccessResponse => false;

        public Conten_MediaType ContentType => Conten_MediaType.ApplicationJson;
    }
}