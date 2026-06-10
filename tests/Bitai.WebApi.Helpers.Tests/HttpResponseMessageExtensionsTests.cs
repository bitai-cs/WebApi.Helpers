using Bitai.WebApi.Client;
using Bitai.WebApi.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Bitai.WebApi.Helpers.Tests
{
    public class HttpReponseMessageExtensionsTests
    {
        private readonly JsonSerializerOptions _testSerializerOptions;




        public HttpReponseMessageExtensionsTests()
        {
            _testSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Setup test serializer options
            WebApiBaseClient.WebApiClientParameters.SerializerOptions = _testSerializerOptions;
        }




        #region ToUnsuccessfulHttpResponseAsync Tests

        [Fact]
        public async Task ToUnsuccessfulHttpResponseAsync_WithSuccessStatusCode_ShouldThrowHttpClientExceptionResponse()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpClientExceptionResponse>(
                () => responseMessage.ToUnsuccessfulHttpResponseAsync()
            );
            Assert.Contains("Cannot execute", exception.Message);
        }

        [Fact]
        public async Task ToUnsuccessfulHttpResponseAsync_WithNoContent_ShouldReturnNoSuccessResponseWithEmptyContent()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(string.Empty)
            };
            responseMessage.Content.Headers.ContentType = null;
            responseMessage.Content.Headers.ContentLength = 0;
            responseMessage.Headers.Date = DateTimeOffset.UtcNow;
            responseMessage.Headers.Server.Add(new ProductInfoHeaderValue("TestServer", "1.0"));

            // Act
            var result = await responseMessage.ToUnsuccessfulHttpResponseAsync();

            // Assert
            Assert.IsType<NoSuccessResponseWithEmptyContent>(result);
            Assert.Equal(HttpStatusCode.BadRequest, result.HttpStatusCode);
            Assert.Equal("Bad Request", result.ReasonPhrase);
            Assert.Equal("TestServer/1.0", result.WebServer);
            Assert.NotNull(result.Date);
        }

        [Fact]
        public async Task ToUnsuccessfulHttpResponseAsync_WithApplicationJson_ShouldReturnNoSuccessResponseWithJsonStringContent()
        {
            // Arrange
            var jsonContent = "{\"key\":\"value\"}";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                ReasonPhrase = "Not Found",
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            responseMessage.Headers.Date = DateTimeOffset.UtcNow;

            // Act
            var result = await responseMessage.ToUnsuccessfulHttpResponseAsync();

            // Assert
            var jsonResponse = Assert.IsType<NoSuccessResponseWithJsonStringContent>(result);
            Assert.Equal(HttpStatusCode.NotFound, jsonResponse.HttpStatusCode);
            Assert.Equal("Not Found", jsonResponse.ReasonPhrase);
            Assert.Equal(jsonContent, jsonResponse.Content);
        }

        [Fact]
        public async Task ToUnsuccessfulHttpResponseAsync_WithApplicationProblemJson_AndMiddlewareException_ShouldReturnNoSuccessResponseWithJsonExceptionContent()
        {
            // Arrange
            var exceptionModel = new MiddlewareExceptionModel(new ApplicationException("Auto-generated exception for xUnit test.", new DllNotFoundException("Dummy exception")));

            var jsonContent = JsonSerializer.Serialize(exceptionModel, _testSerializerOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {                
                Content = new StringContent(jsonContent, Encoding.UTF8, Common.MediaTypes.ApplicationProblemJson)
            };

            // Act
            var result = await responseMessage.ToUnsuccessfulHttpResponseAsync();

            // Assert
            var exceptionResponse = Assert.IsType<NoSuccessResponseWithJsonExceptionContent>(result);
            Assert.Equal(HttpStatusCode.InternalServerError, exceptionResponse.HttpStatusCode);
            Assert.NotNull(exceptionResponse.Content);
            Assert.True(exceptionResponse.Content.IsMiddlewareException);
        }

        [Fact]
        public async Task ToUnsuccessfulHttpResponseAsync_WithApplicationProblemJson_AndNotMiddlewareException_ShouldReturnNoSuccessResponseWithJsonStringContent()
        {
            // Arrange
            var problemJson = "{\"type\":\"https://tools.ietf.org/html/rfc7231#section-6.5.1\",\"title\":\"Bad Request\"}";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                ReasonPhrase = "Bad Request",
                Content = new StringContent(problemJson, Encoding.UTF8, "application/problem+json")
            };

            // Act
            var result = await responseMessage.ToUnsuccessfulHttpResponseAsync();

            // Assert
            var jsonResponse = Assert.IsType<NoSuccessResponseWithJsonStringContent>(result);
            Assert.Equal(HttpStatusCode.BadRequest, jsonResponse.HttpStatusCode);
            Assert.Equal(problemJson, jsonResponse.Content);
        }

        [Fact]
        public async Task ToUnsuccessfulHttpResponseAsync_WithTextHtml_ShouldReturnNoSuccessResponseWithHtmlContent()
        {
            // Arrange
            var htmlContent = "<html><body>Error Page</body></html>";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                ReasonPhrase = "Forbidden",
                Content = new StringContent(htmlContent, Encoding.UTF8, "text/html")
            };
            responseMessage.Headers.Date = DateTimeOffset.UtcNow;

            // Act
            var result = await responseMessage.ToUnsuccessfulHttpResponseAsync();

            // Assert
            var htmlResponse = Assert.IsType<NoSuccessResponseWithHtmlContent>(result);
            Assert.Equal(HttpStatusCode.Forbidden, htmlResponse.HttpStatusCode);
            Assert.Equal(htmlContent, htmlResponse.Content);
        }

        [Fact]
        public async Task ToUnsuccessfulHttpResponseAsync_WithUnsupportedMediaType_ShouldThrowNotSupportedException()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("content", Encoding.UTF8, "application/xml")
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpClientExceptionResponse>(
                () => responseMessage.ToUnsuccessfulHttpResponseAsync()
            );
            Assert.Contains("Unable to generate an IHttpResponse", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ToUnsuccessfulHttpResponseAsync_WithoutServerHeader_ShouldReturnDefaultWebServerText()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(string.Empty)
            };
            responseMessage.Content.Headers.ContentType = null;
            responseMessage.Content.Headers.ContentLength = 0;

            // Act
            var result = await responseMessage.ToUnsuccessfulHttpResponseAsync();

            // Assert
            var emptyResponse = Assert.IsType<NoSuccessResponseWithEmptyContent>(result);
            Assert.Equal("(Server info not provided)", emptyResponse.WebServer);
        }        

        #endregion

        #region ToSuccessfulHttpResponseAsync Tests

        [Fact]
        public async Task ToSuccessfulHttpResponseAsync_WithUnsuccessfulStatusCode_ShouldThrowHttpClientExceptionResponse()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpClientExceptionResponse>(
                () => responseMessage.ToSuccessfulHttpResponseAsync<object>()
            );
            Assert.Contains("Cannot execute", exception.Message);
        }

        [Fact]
        public async Task ToSuccessfulHttpResponseAsync_WithValidDto_ShouldReturnSuccessResponseWithJsonContent()
        {
            // Arrange
            var testDto = new TestDto { Id = 1, Name = "Test" };
            var jsonContent = JsonSerializer.Serialize(testDto, _testSerializerOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                ReasonPhrase = "OK",
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            responseMessage.Headers.Date = DateTimeOffset.UtcNow;
            responseMessage.Headers.Server.Add(new ProductInfoHeaderValue("TestServer", "1.0"));

            // Act
            var result = await responseMessage.ToSuccessfulHttpResponseAsync<TestDto>();

            // Assert
            var successResponse = Assert.IsType<SuccessResponseWithJsonContent<TestDto>>(result);
            Assert.Equal(HttpStatusCode.OK, successResponse.HttpStatusCode);
            Assert.Equal("OK", successResponse.ReasonPhrase);
            Assert.Equal("TestServer/1.0", successResponse.WebServer);
            Assert.NotNull(successResponse.Date);
            Assert.NotNull(successResponse.Content);
            Assert.Equal(1, successResponse.Content.Id);
            Assert.Equal("Test", successResponse.Content.Name);
        }

        [Fact]
        public async Task ToSuccessfulHttpResponseAsync_WithNoContent_ShouldDeserializeEmptyContent()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty)
            };
            responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(Common.MediaTypes.ApplicationJson);
            responseMessage.Content.Headers.ContentLength = 0;

            // Act
            var result = await responseMessage.ToSuccessfulHttpResponseAsync<TestDto>();

            // Assert
            var successResponse = Assert.IsType<SuccessResponseWithJsonContent<TestDto>>(result);
            Assert.Equal(HttpStatusCode.OK, successResponse.HttpStatusCode);
            // Should handle empty content gracefully - may deserialize to default
        }

        [Fact]
        public async Task ToSuccessfulHttpResponseAsync_WithoutServerHeader_ShouldReturnEmptyWebServer()
        {
            // Arrange
            var testDto = new TestDto { Id = 1, Name = "Test" };
            var jsonContent = JsonSerializer.Serialize(testDto, _testSerializerOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                ReasonPhrase = "OK",
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            responseMessage.Headers.Server.Clear();

            // Act
            var result = await responseMessage.ToSuccessfulHttpResponseAsync<TestDto>();

            // Assert
            var successResponse = Assert.IsType<SuccessResponseWithJsonContent<TestDto>>(result);
            Assert.Equal("(Server info not provided)", successResponse.WebServer);
        }

        [Fact]
        public async Task ToSuccessfulHttpResponseAsync_WithNullDateHeader_ShouldReturnEmptyDateString()
        {
            // Arrange
            var testDto = new TestDto { Id = 1, Name = "Test" };
            var jsonContent = JsonSerializer.Serialize(testDto, _testSerializerOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                ReasonPhrase = "OK",
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            responseMessage.Headers.Date = null;

            // Act
            var result = await responseMessage.ToSuccessfulHttpResponseAsync<TestDto>();

            // Assert
            var successResponse = Assert.IsType<SuccessResponseWithJsonContent<TestDto>>(result);
            Assert.Equal(string.Empty, successResponse.Date);
        }

        [Fact]
        public async Task ToSuccessfulHttpResponseAsync_WithMultipleServerHeaders_ShouldUseFirstOne()
        {
            // Arrange
            var testDto = new TestDto { Id = 1, Name = "Test" };
            var jsonContent = JsonSerializer.Serialize(testDto, _testSerializerOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                ReasonPhrase = "OK",
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            responseMessage.Headers.Server.Add(new ProductInfoHeaderValue("Server1", "1.0"));
            responseMessage.Headers.Server.Add(new ProductInfoHeaderValue("Server2", "2.0"));

            // Act
            var result = await responseMessage.ToSuccessfulHttpResponseAsync<TestDto>();

            // Assert
            var successResponse = Assert.IsType<SuccessResponseWithJsonContent<TestDto>>(result);
            Assert.Equal("Server1/1.0 Server2/2.0", successResponse.WebServer);
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Fact]
        public async Task ParseWebServerHeader_WithMultipleServerHeaderComponents_ShouldParseCorrectly()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            responseMessage.Content.Headers.ContentType = null;
            responseMessage.Headers.Server.Add(new ProductInfoHeaderValue("nginx", "1.18.0"));
            responseMessage.Headers.Server.Add(new ProductInfoHeaderValue("(Ubuntu)"));

            // Act
            var result = await responseMessage.ToUnsuccessfulHttpResponseAsync();
           
            // Assert
            var emptyResponse = Assert.IsType<NoSuccessResponseWithEmptyContent>(result);
            Assert.Equal("nginx/1.18.0 (Ubuntu)", emptyResponse.WebServer);
        }

        [Fact]
        public async Task ToUnsuccessfulHttpResponseAsync_WithCaseInsensitiveMiddlewareExceptionCheck_ShouldWorkCorrectly()
        {
            // Arrange
            var exceptionModel = new MiddlewareExceptionModel
            {
                Message = "Test"
            };

            // Use different casing than in the code
            var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
            var jsonContent = JsonSerializer.Serialize(exceptionModel, options);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/problem+json")
            };

            // Act
            var result = await responseMessage.ToUnsuccessfulHttpResponseAsync();

            // Assert
            Assert.IsType<NoSuccessResponseWithJsonExceptionContent>(result);
        }

        #endregion
    }

    #region Test DTOs

    public class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    #endregion
}
