using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Bitai.WebApi.Common;

namespace Bitai.WebApi.Client {
	public class NoSuccessResponseWithHtmlContent :IHttpResponse<string> {
		public NoSuccessResponseWithHtmlContent(string html, HttpStatusCode httpStatusCode, string reasonPhrase, string webServer, string date) {
			this.Content = html;
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

		public Conten_MediaType ContentType => Conten_MediaType.TextHtml;
	}
}