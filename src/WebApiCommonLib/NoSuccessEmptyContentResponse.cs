using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace WebApiCommonLib {
	public class NoSuccessEmptyContentResponse :IHttpResponse<string> {
		public NoSuccessEmptyContentResponse(HttpStatusCode httpStatusCode, string reasonPhrase, string webServer, string date) {
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

		public ContenType ContentType => ContenType.NoContent;




		//public string ToHtmlReport() {
		//	var _template = "<p><b>Server:</b>&nbsp{1}</p><p><b>Date:</b>&nbsp{2}</p>";
		//	var _report = string.Format(_template, System.Environment.NewLine, this.WebServer, this.Date);

		//	return _report;
		//}

		//public string ToHtmlReport(bool includeStackTrace, bool includeInnerErrors) {
		//	return ToHtmlReport();
		//}

		//public string ToStringReport() {
		//	var _template = "Server: {1}{0}Date: {2}{0}";
		//	var _report = string.Format(_template, System.Environment.NewLine, this.WebServer, this.Date);

		//	return _report;
		//}

		//public string ToStringReport(bool includeStackTrace, bool includeInnerErrors) {
		//	return ToStringReport();
		//}
	}
}