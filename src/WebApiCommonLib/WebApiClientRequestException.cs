using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace WebApiCommonLib {
	//public class WebApiClientRequestException<TContent> :Exception {
	public class WebApiClientRequestException :Exception {
		public WebApiClientRequestException(string message, IHttpResponse relatedHttpResponse) : base(message) {
			NoSuccessResponse = relatedHttpResponse;
		}
		//public WebApiClientRequestException(string message, ContenType httpResponseContentType, IHttpResponse relatedHttpResponse) : base(message) {
		//	HttpResponseContentType = httpResponseContentType;
		//	NoSuccessResponse = relatedHttpResponse;
		//}



		//public ContenType HttpResponseContentType { get; }
		public IHttpResponse NoSuccessResponse { get; }
	}
}