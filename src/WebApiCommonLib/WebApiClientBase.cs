using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebApiCommonLib;

namespace WebApiCommonLib {

	public abstract partial class WebApiClientBase<TDTO> {
		protected WebApiClientBase(string webApiUrl) {
			if (string.IsNullOrEmpty(webApiUrl))
				throw new ArgumentException("Debe de especificar el URI del Web Api.");

			if (!webApiUrl.EndsWith("/"))
				throw new ArgumentException("El URL base del Web Api debe terminar en '/'.");

			WebApiUrl = webApiUrl;
		}




		protected readonly string WebApiUrl;




		public bool IsNoSuccessResponse(IHttpResponse httpResponse) {
			return !httpResponse.IsSuccessResponse;
		}

		public void ThrowWebApiClientRequestException(string exceptionMessage, IHttpResponse httpResponse) {
			throw GetWebApiClientRequestException(exceptionMessage, httpResponse);
		}

		public Exception GetWebApiClientRequestException(string exceptionMessage, IHttpResponse httpResponse) {
			if (httpResponse.IsSuccessResponse)
				throw new InvalidOperationException("La respuesta del Web Api es satisfactoria. No se puede inferir un objeto Exception de IHttpResponse.");

			return new WebApiCommonLib.WebApiClientRequestException(exceptionMessage, httpResponse);
			////VBG: Old version code
			//Exception _exception = null;
			//switch (httpResponse.ContentType) {
			//	case ContenType.NoContent:
			//		_exception = new WebApiCommonLib.WebApiClientRequestException(exceptionMessage, ContenType.NoContent, (IHttpResponse<string>)httpResponse);
			//		break;
			//	case ContenType.TextHtml:
			//		_exception = new WebApiCommonLib.WebApiClientRequestException<string>(exceptionMessage, ContenType.TextHtml, (IHttpResponse<string>)httpResponse);
			//		break;
			//	case ContenType.AppExceptionJson:
			//		_exception = new WebApiCommonLib.WebApiClientRequestException<ExceptionJsonFormat>(exceptionMessage, ContenType.AppExceptionJson, (IHttpResponse<ExceptionJsonFormat>)httpResponse);
			//		break;
			//}			
		}

		public TDTO GetDTOFromResponseContent(IHttpResponse httpResponse) {
			var _s = (SuccessJsonContentResponse<TDTO>)httpResponse;
			return _s.Content;
		}

		public Task<TDTO> GetDTOFromResponseContentAsync(IHttpResponse httpResponse) {
			return Task.Run(() => GetDTOFromResponseContent(httpResponse));
		}

		public IEnumerable<TDTO> GetEnumerableDTOFromResponseContent(IHttpResponse httpResponse) {
			var _s = (SuccessJsonContentResponse<IEnumerable<TDTO>>)httpResponse;
			return _s.Content;
		}

		public Task<IEnumerable<TDTO>> GetEnumerableDTOFromResponseContentAsync(IHttpResponse httpResponse) {
			return Task.Run(() => GetEnumerableDTOFromResponseContent(httpResponse));
		}





		protected HttpClient GetHttpClient(bool clearDefaultRequestHeaders = true, Header_AcceptType headerAcceptType = Header_AcceptType.ApplicationJson) {
			if (WebApiClientStartup.ClientRequestTimeOut == 0)
				throw new InvalidOperationException("No se ha inicializado el parametro WebApiClientStartup.ClientRequestTimeOut");

			if (WebApiClientStartup.MaxResponseContentBufferSize == 0)
				throw new InvalidOperationException("No se ha inicializado el parametro WebApiClientStartup.MaxResponseContentBufferSize");

#if DEBUG
			if (System.Net.ServicePointManager.ServerCertificateValidationCallback == null) {
				System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors) {
					return true;
				};
			}
#endif
			var _client = new HttpClient {
				BaseAddress = new Uri(this.WebApiUrl),
				Timeout = new TimeSpan(0, 0, WebApiClientStartup.ClientRequestTimeOut),
				MaxResponseContentBufferSize = WebApiClientStartup.MaxResponseContentBufferSize
			};

			if (clearDefaultRequestHeaders)
				_client.DefaultRequestHeaders.Clear();

			switch (headerAcceptType) {
				case Header_AcceptType.ApplicationJson:
					_client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(WebApiClientStartup.MimeTypes.MimeType_AppJson));
					break;
				default:
					throw new Exception("No se ha especificado el tipo de Header Accept.");
			}

			return _client;
		}

		protected async Task<IHttpResponse> ParseHttpResponseToNoSuccessResponseAsync(string requestUri, HttpResponseMessage responseMessage) {
			//Validar StatusCode entre 200 y 299
			if (responseMessage.StatusCode >= System.Net.HttpStatusCode.OK && responseMessage.StatusCode < System.Net.HttpStatusCode.MultipleChoices)
				throw new InvalidOperationException("El código de estado de la respuesta es satisfactorio (200-299). No se puede realizar la operación ParseHttpResponseToNoSuccessResponseAsync.");

			var _statusCode = responseMessage.StatusCode;
			var _reasonPhrase = responseMessage.ReasonPhrase;
			var _webServer = responseMessage.Headers.Server.ToArray()[0].Product.ToString();
			var _date = responseMessage.Headers.Date.HasValue ? responseMessage.Headers.Date.Value.LocalDateTime.ToString() : "?";

			string _mediaType;
			if (responseMessage.Content.Headers.ContentLength.Value.Equals(0))
				_mediaType = WebApiClientStartup.MimeTypes.MimeType_NoContent;
			else
				_mediaType = responseMessage.Content.Headers.ContentType.MediaType.ToLower();

			switch (_mediaType) {
				case WebApiClientStartup.MimeTypes.MimeType_NoContent:
					return new WebApiCommonLib.NoSuccessEmptyContentResponse(_statusCode, _reasonPhrase, _webServer, _date);

				case WebApiClientStartup.MimeTypes.MimeType_AppJson:
					var _jsonContent = await responseMessage.Content.ReadAsStringAsync();

					if (!(_jsonContent.IndexOf("IsExceptionJsonFormat", comparisonType: StringComparison.OrdinalIgnoreCase).Equals(-1))) {
						var _deserializedContent = JsonConvert.DeserializeObject<WebApiCommonLib.ExceptionJsonFormat>(_jsonContent);

						return new WebApiCommonLib.NoSuccessJsonErrorContentResponse(_deserializedContent, _statusCode, _reasonPhrase, _webServer, _date);
					}
					else {
						return new WebApiCommonLib.NoSuccessJsonStringContentResponse(_jsonContent, ContenType.AppJson, _statusCode, _reasonPhrase, _webServer, _date);
					}

				case WebApiClientStartup.MimeTypes.MimeType_AppProblemJson:
					var _problemJsonContent = await responseMessage.Content.ReadAsStringAsync();

					return new WebApiCommonLib.NoSuccessJsonStringContentResponse(_problemJsonContent, ContenType.AppProblemJson, _statusCode, _reasonPhrase, _webServer, _date);

				case WebApiClientStartup.MimeTypes.MimeType_TextHtml:
					var _htmlContent = await responseMessage.Content.ReadAsStringAsync();

					return new WebApiCommonLib.NoSuccessHtmlContentResponse(_htmlContent, _statusCode, _reasonPhrase, _webServer, _date);

				default: //En caso de llegar a este caso, se debería implementar el manejo para el MIME desconocido. Por ahora se dispara un error informando el caso.
					throw new NotSupportedException(string.Format("No se puede devolver una vista para las respuestas de error de solicitud http cuyo contenido en la respuesta sea el tipo \"{0}\". Se debe de implementar el soporte para ese tipo MIME en el Assembly: NetSqlAzMan.CustomBussinessLogic Clase: LdapWebApiClientHelpers.BaseHelpers Método: getHttpWebApiRequestException", _mediaType));
			}
		}

		protected async Task<IHttpResponse> ParseHttpResponseToSuccessResponseWithDTOContentAsync(HttpResponseMessage responseMessage) {
			//Validar StatusCode entre 200 y 299
			if (!(responseMessage.StatusCode >= System.Net.HttpStatusCode.OK && responseMessage.StatusCode < System.Net.HttpStatusCode.MultipleChoices))
				throw new InvalidOperationException("El código de estado de la respuesta no es satisfactorio. No se puede realizar la operación ParseHttpResponseToSuccessResponseWithDTOContentAsync.");

			var _statusCode = responseMessage.StatusCode;

			var _reasonPhrase = responseMessage.ReasonPhrase;

			string _mediaType;
			if (responseMessage.Content.Headers.ContentLength.Value.Equals(0))
				_mediaType = WebApiClientStartup.MimeTypes.MimeType_NoContent;
			else
				_mediaType = responseMessage.Content.Headers.ContentType.MediaType.ToLower();

			string _jsonContent = await responseMessage.Content.ReadAsStringAsync();

			var _deserializedContent = await Task.Run(() => {
#if DEBUG
				var _dto = JsonConvert.DeserializeObject<TDTO>(_jsonContent);
				return _dto;
#else
				return JsonConvert.DeserializeObject<TDTO>(_jsonContent);
#endif
			});

			var _successJsonContentResponse = new SuccessJsonContentResponse<TDTO>() {
				HttpStatusCode = _statusCode,
				ReasonPhrase = _reasonPhrase,
				WebServer = responseMessage.Headers.Server.ToArray()[0].Product.ToString(),
				Date = responseMessage.Headers.Date.HasValue ? responseMessage.Headers.Date.Value.LocalDateTime.ToString() : "?",
				Content = _deserializedContent
			};

			return _successJsonContentResponse;
		}

		protected async Task<IHttpResponse> ParseHttpResponseToSuccessResponseWithEnumerableDTOContentAsync(HttpResponseMessage responseMessage) {
			//Validar StatusCode entre 200 y 299
			if (!(responseMessage.StatusCode >= System.Net.HttpStatusCode.OK && responseMessage.StatusCode < System.Net.HttpStatusCode.MultipleChoices))
				throw new InvalidOperationException("El código de estado de la respuesta no es satisfactorio. No se puede realizar la operación ParseHttpResponseToSuccessResponseWithDTOContentAsync.");

			var _statusCode = responseMessage.StatusCode;

			var _reasonPhrase = responseMessage.ReasonPhrase;

			string _mediaType;
			if (responseMessage.Content.Headers.ContentLength.Value.Equals(0))
				_mediaType = WebApiClientStartup.MimeTypes.MimeType_NoContent;
			else
				_mediaType = responseMessage.Content.Headers.ContentType.MediaType.ToLower();

			string _jsonContent = await responseMessage.Content.ReadAsStringAsync();

			var _deserializedContent = await Task.Run(() => JsonConvert.DeserializeObject<IEnumerable<TDTO>>(_jsonContent));

			var _successJsonContentResponse = new SuccessJsonContentResponse<IEnumerable<TDTO>>() {
				HttpStatusCode = _statusCode,
				ReasonPhrase = _reasonPhrase,
				WebServer = responseMessage.Headers.Server.ToArray()[0].Product.ToString(),
				Date = responseMessage.Headers.Date.HasValue ? responseMessage.Headers.Date.Value.LocalDateTime.ToString() : "?",
				Content = _deserializedContent
			};

			return _successJsonContentResponse;
		}

		protected System.Net.Http.StringContent GetStringContentFromObject(object dto, ContentEncoding encoding = ContentEncoding.UTF8, ContenType mediaType = ContenType.AppJson) {
			Encoding _encoding;
			switch (encoding) {
				case ContentEncoding.UTF8:
					_encoding = Encoding.UTF8;
					break;
				default:
					throw new NotImplementedException($"encoding: {encoding} not implemented.");
			}

			string _mediaType;
			switch (mediaType) {
				case ContenType.AppJson:
					_mediaType = "application/json";
					break;
				default:
					throw new NotImplementedException($"mediatype: {mediaType} not implemented.");
			}

			return new System.Net.Http.StringContent(JsonConvert.SerializeObject(dto), _encoding, _mediaType);
		}
		


		private string getFullUri(string requestUri) {
			return WebApiUrl + "/" + requestUri;
		}		
	}
}
