using System;
using System.Collections.Generic;
using System.Text;

namespace WebApiCommonLib {
	public interface IHttpResponse {
		bool IsSuccessResponse { get; }
		System.Net.HttpStatusCode HttpStatusCode { get; }
		string WebServer { get; }
		string Date { get; }
		string ReasonPhrase { get; }
		ContenType ContentType { get; }
	}

	public interface IHttpResponse<TContent> :IHttpResponse {

		TContent Content { get; }
	}
}
