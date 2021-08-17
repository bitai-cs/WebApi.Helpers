using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Bitai.WebApi.Common;

namespace Bitai.WebApi.Client {
	public class NoSuccessResponseWithJsonStringContent : IHttpResponse<string> {
		public NoSuccessResponseWithJsonStringContent(string json, Content_MediaType contentType, HttpStatusCode httpStatusCode, string reasonPhrase, string webServer, string date) {
			if (!(contentType == Content_MediaType.ApplicationJson | contentType == Content_MediaType.ApplicationProblemJson))
				throw new InvalidOperationException($"The ContentType equal to \"{contentType.ToString()}\" cannot be assigned to this object");

			this.Content = json;
			this.ContentMediaType = contentType;
			this.HttpStatusCode = httpStatusCode;
			this.ReasonPhrase = reasonPhrase;
			this.WebServer = webServer;
			this.Date = date;
		}



		public HttpStatusCode HttpStatusCode { get; set; }

		public string WebServer { get; set; }

		public string Date { get; set; }

		public string Content { get; set; }

		public string ReasonPhrase { get; set; }

		public bool IsSuccessResponse => false;

		public Content_MediaType ContentMediaType { get; }
	}
}