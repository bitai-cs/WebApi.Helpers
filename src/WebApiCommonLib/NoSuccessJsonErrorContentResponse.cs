using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace WebApiCommonLib {
	public class NoSuccessJsonErrorContentResponse :IHttpResponse<ExceptionJsonFormat> {
		public NoSuccessJsonErrorContentResponse(ExceptionJsonFormat exception, HttpStatusCode httpStatusCode, string reasonPhrase, string webServer, string date) {
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

		public ExceptionJsonFormat Content { get; set; }

		public bool IsSuccessResponse => false;

		public ContenType ContentType => ContenType.AppJson;
	}
}
