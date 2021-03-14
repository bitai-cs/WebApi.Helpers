using System;
using System.Collections.Generic;
using System.Text;

namespace WebApiCommonLib {
	public enum Header_AcceptType {
		ApplicationJson
	}

	public enum ContenType {
		NoContent,
		/*ExceptionJson,*/
		TextHtml,
		AppJson,
		AppProblemJson
	}

	public enum ContentEncoding {
		UTF8
	}
}