using System;
using System.Collections.Generic;
using System.Linq;

namespace Bitai.WebApi.Server
{
    /// <summary>
    /// Error catched in the exception handling middleware (<see cref="Server.ExceptionHandlingMiddleware"/>).
    /// </summary>
    public class MiddlewareExceptionModel
    {
        #region Properties
        public bool IsMiddlewareException => true;
        public string Type { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string StackTrace { get; set; }
        public IEnumerable<string> ErrorDetail { get; set; }
        public MiddlewareExceptionModel InnerMiddlewareException { get; set; }
        #endregion


        #region Constructors
        /// <summary>
        /// Constructor. Commonly used by the serializer.
        /// </summary>
        public MiddlewareExceptionModel()
        {
            ErrorDetail = new List<string>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="exception">Error that will provide the data to initialize properties</param>
        public MiddlewareExceptionModel(Exception exception) : this()
        {
            Type = exception.GetType().FullName;
            Message = exception.Message;
            Source = exception.Source;
            StackTrace = exception.StackTrace;

            var _detail = new List<string>();
            foreach (var _key in exception.Data.Keys)
            {
                _detail.Add(string.Format("{0}: {1}", _key.ToString(), exception.Data[_key] ?? "null"));
            }
            ErrorDetail = _detail;

            if (exception.InnerException == null)
                return;

            if (exception.InnerException != null)
                InnerMiddlewareException = new MiddlewareExceptionModel(exception.InnerException);
        }
        #endregion


        #region Public methods
        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Type, this.Message);
        }

        public string ToStringReport(bool includeStackTrace, bool includeInnerErrors)
        {
            var _template = "{0}Description: {1}: {2}\r\n{0}Source: {3}\r\n{0}Details: {4}\r\n" + (includeStackTrace ? "{0}Stack Trace: {5}\r\n" : string.Empty);

            var _details = string.Empty;
            foreach (var _i in this.ErrorDetail)
            {
                _details += _i + " | ";
            }
            if (this.ErrorDetail.Count() > 0)
                _details = _details.Substring(0, _details.Length - 3);

            string _report;
            if (includeStackTrace)
                _report = string.Format(_template, string.Empty, this.Type, this.Message, this.Source, _details, this.StackTrace);
            else
                _report = string.Format(_template, string.Empty, this.Type, this.Message, this.Source, _details);

            if (includeInnerErrors)
            {
                var _innerError = this.InnerMiddlewareException;
                var _tabCount = 0;
                while (_innerError != null)
                {
                    _report += new string('\t', _tabCount) + "Inner Exception:\n\r\n\r";

                    _tabCount++;

                    _details = string.Empty;
                    foreach (var _i in _innerError.ErrorDetail)
                    {
                        _details += _i + " | ";
                    }
                    if (_innerError.ErrorDetail.Count() > 0)
                        _details = _details.Substring(0, _details.Length - 3);

                    if (includeStackTrace)
                        _report += string.Format(_template, new string('\t', _tabCount), _innerError.Type, _innerError.Message, _innerError.Source, _details, _innerError.StackTrace);
                    else
                        _report += string.Format(_template, new string('\t', _tabCount), _innerError.Type, _innerError.Message, _innerError.Source, _details);

                    _innerError = _innerError.InnerMiddlewareException;
                }
            }
            else
            {
                if (this.InnerMiddlewareException != null)
                    _report += "Inner Exception: Si, existe error anidado.";
            }

            return _report;
        }

        public string ToStringReport()
        {
            return ToStringReport(true, true);
        }

        public string ToHtmlReport()
        {
            throw new NotImplementedException("This will be implemented in future versions.");
        }

        public string ToHtmlReport(bool includeStackTrace, bool includeInnerErrors)
        {
            throw new NotImplementedException("This will be implemented in future versions.");
        }
        #endregion
    }
}