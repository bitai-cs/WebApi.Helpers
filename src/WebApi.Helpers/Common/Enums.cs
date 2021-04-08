using System;
using System.Collections.Generic;
using System.Text;

namespace Bitai.WebApi.Common {
	public enum Header_AcceptType {
		ApplicationJson
	}

	public enum Conten_MediaType {
		NoContent,
		TextHtml,
		ApplicationJson,
		ApplicationProblemJson
	}

	public enum Content_Encoding {
		UTF8
	}
}