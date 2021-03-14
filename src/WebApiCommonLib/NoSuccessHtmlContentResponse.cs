using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace WebApiCommonLib {
	public class NoSuccessHtmlContentResponse :IHttpResponse<string> {
		public NoSuccessHtmlContentResponse(string html, HttpStatusCode httpStatusCode, string reasonPhrase, string webServer, string date) {
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

		public ContenType ContentType => ContenType.TextHtml;
	}
}