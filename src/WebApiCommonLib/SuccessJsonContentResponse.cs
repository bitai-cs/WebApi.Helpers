using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace WebApiCommonLib {
	public class SuccessJsonContentResponse<TJSON> :IHttpResponse<TJSON> {
		public HttpStatusCode HttpStatusCode { get; set; }

		public string WebServer { get; set; }

		public string Date { get; set; }

		public TJSON Content { get; set; }

		public string ReasonPhrase { get; set; }

		public bool IsSuccessResponse => true;

		public ContenType ContentType => ContenType.AppJson;



		//public string ToStringReport(bool includeStackTrace, bool includeInnerErrors) {
		//	var _template = "{0}Description: {1}: {2}\r\n{0}Source: {3}\r\n{0}Details: {4}\r\n" + (includeStackTrace ? "{0}Stack Trace: {5}\r\n" : string.Empty);

		//	var _details = string.Empty;
		//	foreach (var _i in this.Content.ErrorDetail) {
		//		_details += _i + " | ";
		//	}
		//	if (this.Content.ErrorDetail.Count() > 0)
		//		_details = _details.Substring(0, _details.Length - 3);

		//	string _report;
		//	if (includeStackTrace)
		//		_report = string.Format(_template, string.Empty, this.Content.Type, this.Content.Message, this.Content.Source, _details, this.Content.StackTrace);
		//	else
		//		_report = string.Format(_template, string.Empty, this.Content.Type, this.Content.Message, this.Content.Source, _details);

		//	if (includeInnerErrors) {
		//		var _innerError = this.Content.InnerErrorSummary;
		//		var _tabCount = 0;
		//		while (_innerError != null) {
		//			_report += new string('\t', _tabCount) + "Inner Exception:\n\r\n\r";

		//			_tabCount++;

		//			_details = string.Empty;
		//			foreach (var _i in _innerError.ErrorDetail) {
		//				_details += _i + " | ";
		//			}
		//			if (_innerError.ErrorDetail.Count() > 0)
		//				_details = _details.Substring(0, _details.Length - 3);

		//			if (includeStackTrace)
		//				_report += string.Format(_template, new string('\t', _tabCount), _innerError.Type, _innerError.Message, _innerError.Source, _details, _innerError.StackTrace);
		//			else
		//				_report += string.Format(_template, new string('\t', _tabCount), _innerError.Type, _innerError.Message, _innerError.Source, _details);

		//			_innerError = _innerError.InnerErrorSummary;
		//		}
		//	}
		//	else {
		//		if (this.Content.InnerErrorSummary != null)
		//			_report += "Inner Exception: Si, existe error anidado.";
		//	}

		//	return _report;
		//}

		//public string ToStringReport() {
		//	return ToStringReport(true, true);
		//}

		//public string ToHtmlReport() {
		//	throw new NotImplementedException();
		//}

		//public string ToHtmlReport(bool includeStackTrace, bool includeInnerErrors) {
		//	throw new NotImplementedException();
		//}
	}
}
