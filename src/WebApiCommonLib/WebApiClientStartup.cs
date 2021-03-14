using System;
using System.Collections.Generic;
using System.Text;

namespace WebApiCommonLib {
	public static class WebApiClientStartup {
		public class MimeTypes {
			public const string MimeType_AppJson = "application/json";
			public const string MimeType_AppProblemJson = "application/problem+json";			
			public const string MimeType_TextHtml = "text/html";
			public const string MimeType_NoContent = "";
		}

		/// <summary>
		/// Constructor Static que inicializa los parametros con valores por defecto
		/// </summary>
		static WebApiClientStartup() {
			ClientRequestTimeOut = 3 * 60;
			MaxResponseContentBufferSize = 1 * 1024 * 1024;
		}

		static public int ClientRequestTimeOut { get; set; }
		static public long MaxResponseContentBufferSize { get; set; }
	}
}
