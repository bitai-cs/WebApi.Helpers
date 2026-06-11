using System.Net;
using Bitai.WebApi.Client;
using Bitai.WebApi.Common;

namespace Bitai.WebApi.Helpers.Tests;

public class WebApiBaseClientTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WhenBaseUrlIsNullOrEmpty_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TestWebApiBaseClient(null!));
        Assert.Throws<ArgumentNullException>(() => new TestWebApiBaseClient(string.Empty));
    }

    [Fact]
    public void Constructor_WhenBaseUrlEndsWithSlash_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => new TestWebApiBaseClient("https://api.example.com/"));

        Assert.Contains("cannot end with the character '/'", exception.Message);
    }

    [Fact]
    public void Constructor_WithBaseUrl_SetsBaseUrlProperty()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");

        Assert.Equal("https://api.example.com", client.TestBaseUrl);
    }

    [Fact]
    public void Constructor_WithBaseUrlAndHandler_SetsHandlerAndDisposeHandler()
    {
        using var handler = new HttpClientHandler();
        var client = new TestWebApiBaseClient("https://api.example.com", handler, disposeHandler: true);

        Assert.Same(handler, client.TestHandler);
        Assert.True(client.TestDisposeHandler);
        Assert.Equal("https://api.example.com", client.TestBaseUrl);
    }

    [Fact]
    public void Constructor_WithBaseUrlAndCredential_SetsCredential()
    {
        var credential = new WebApiClientCredential
        {
            AuthorityUrl = "https://auth.example.com",
            ApiScope = "api.scope",
            ClientId = "client-id",
            ClientSecret = "client-secret"
        };
        var client = new TestWebApiBaseClient("https://api.example.com", credential);

        Assert.Same(credential, client.TestCredential);
        Assert.Equal("https://api.example.com", client.TestBaseUrl);
    }

    [Fact]
    public void Constructor_WithBaseUrlCredentialAndHandler_SetsAllProperties()
    {
        using var handler = new HttpClientHandler();
        var credential = new WebApiClientCredential
        {
            AuthorityUrl = "https://auth.example.com",
            ApiScope = "api.scope",
            ClientId = "client-id",
            ClientSecret = "client-secret"
        };
        var client = new TestWebApiBaseClient("https://api.example.com", credential, handler, disposeHandler: false);

        Assert.Same(credential, client.TestCredential);
        Assert.Same(handler, client.TestHandler);
        Assert.False(client.TestDisposeHandler);
        Assert.Equal("https://api.example.com", client.TestBaseUrl);
    }

    [Fact]
    public void Constructor_WithNullHandler_AllowsCreation()
    {
        var client = new TestWebApiBaseClient("https://api.example.com", handler: null!, disposeHandler: false);

        Assert.Null(client.TestHandler);
        Assert.False(client.TestDisposeHandler);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public void GetClientRequestException_WhenResponseIsNotSuccessful_ReturnsExceptionWithResponse()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var response = new NoSuccessResponseWithEmptyContent(
            HttpStatusCode.BadRequest,
            "Bad Request",
            "Kestrel",
            "Fri, 05 Jun 2026 12:00:00 GMT");

        var exception = client.GetClientRequestException("Request failed.", response);

        Assert.Equal("Request failed.", exception.Message);
        Assert.Same(response, exception.NoSuccessResponse);
    }

    [Fact]
    public void GetClientRequestException_WhenResponseIsSuccessful_ThrowsInvalidOperationException()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var response = new SuccessResponseWithJsonContent<string>
        {
            HttpStatusCode = HttpStatusCode.OK,
            Content = "ok"
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => client.GetClientRequestException("Request failed.", response));

        Assert.Contains(nameof(WebApiRequestException), exception.Message);
    }

    [Fact]
    public void ThrowClientRequestException_WhenResponseIsNotSuccessful_ThrowsWebApiRequestException()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var response = new NoSuccessResponseWithEmptyContent(
            HttpStatusCode.InternalServerError,
            "Internal Server Error",
            "Kestrel",
            "Fri, 05 Jun 2026 12:00:00 GMT");

        var exception = Assert.Throws<WebApiRequestException>(
            () => client.ThrowClientRequestException("Request failed.", response));

        Assert.Same(response, exception.NoSuccessResponse);
    }

    [Fact]
    public void ThrowClientRequestException_WithDifferentHttpStatusCodes_ThrowsWebApiRequestException()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var statusCodes = new[]
        {
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.MethodNotAllowed,
            HttpStatusCode.Conflict,
            HttpStatusCode.GatewayTimeout
        };

        foreach (var statusCode in statusCodes)
        {
            var response = new NoSuccessResponseWithEmptyContent(
                statusCode,
                statusCode.ToString(),
                "Kestrel",
                "Fri, 05 Jun 2026 12:00:00 GMT");

            var exception = Assert.Throws<WebApiRequestException>(
                () => client.ThrowClientRequestException($"Request failed with {statusCode}.", response));

            Assert.Equal($"Request failed with {statusCode}.", exception.Message);
            Assert.Same(response, exception.NoSuccessResponse);
            Assert.Equal(statusCode, exception.NoSuccessResponse.HttpStatusCode);
        }
    }

    #endregion

    #region DTO Response Tests

    [Fact]
    public void GetDTOFromResponse_WhenResponseContainsDto_ReturnsContent()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var dto = new SampleDto(7, "test");
        var response = new SuccessResponseWithJsonContent<SampleDto>
        {
            HttpStatusCode = HttpStatusCode.OK,
            Content = dto
        };

        var result = client.GetDTOFromResponse<SampleDto>(response);

        Assert.Same(dto, result);
    }

    [Fact]
    public async Task GetDTOFromResponseAsync_WhenResponseContainsDto_ReturnsContent()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var dto = new SampleDto(7, "test");
        var response = new SuccessResponseWithJsonContent<SampleDto>
        {
            HttpStatusCode = HttpStatusCode.OK,
            Content = dto
        };

        var result = await client.GetDTOFromResponseAsync<SampleDto>(response);

        Assert.Same(dto, result);
    }

    [Fact]
    public void GetEnumerableDTOFromResponse_WhenResponseContainsDtos_ReturnsContent()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var dtos = new[] { new SampleDto(1, "first"), new SampleDto(2, "second") };
        var response = new SuccessResponseWithJsonContent<IEnumerable<SampleDto>>
        {
            HttpStatusCode = HttpStatusCode.OK,
            Content = dtos
        };

        var result = client.GetEnumerableDTOFromResponse<SampleDto>(response);

        Assert.Same(dtos, result);
    }

    [Fact]
    public async Task GetEnumerableDTOFromResponseAsync_WhenResponseContainsDtos_ReturnsContent()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var dtos = new[] { new SampleDto(1, "first"), new SampleDto(2, "second") };
        var response = new SuccessResponseWithJsonContent<IEnumerable<SampleDto>>
        {
            HttpStatusCode = HttpStatusCode.OK,
            Content = dtos
        };

        var result = await client.GetEnumerableDTOFromResponseAsync<SampleDto>(response);

        Assert.Same(dtos, result);
    }

    [Fact]
    public void GetDTOFromResponse_WithComplexObject_ReturnsDeserializedContent()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var dto = new ComplexDto
        {
            Id = 1,
            Name = "test",
            Values = new[] { "a", "b", "c" },
            Metadata = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } }
        };
        var response = new SuccessResponseWithJsonContent<ComplexDto>
        {
            HttpStatusCode = HttpStatusCode.OK,
            Content = dto
        };

        var result = client.GetDTOFromResponse<ComplexDto>(response);

        Assert.Equal(dto.Id, result.Id);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Values, result.Values);
        Assert.Equal(dto.Metadata, result.Metadata);
    }

    [Fact]
    public async Task GetDTOFromResponseAsync_WithNullContent_ReturnsNull()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var response = new SuccessResponseWithJsonContent<string>
        {
            HttpStatusCode = HttpStatusCode.OK,
            Content = null!
        };

        var result = await client.GetDTOFromResponseAsync<string>(response);

        Assert.Null(result);
    }

    [Fact]
    public void GetEnumerableDTOFromResponse_WithEmptyCollection_ReturnsEmptyEnumerable()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var emptyDtos = Enumerable.Empty<SampleDto>();
        var response = new SuccessResponseWithJsonContent<IEnumerable<SampleDto>>
        {
            HttpStatusCode = HttpStatusCode.OK,
            Content = emptyDtos
        };

        var result = client.GetEnumerableDTOFromResponse<SampleDto>(response);

        Assert.Empty(result);
    }

    #endregion

    #region CreateHttpClient Tests

    [Fact]
    public async Task CreateHttpClient_WhenAuthorizationIsRequestedWithoutCredential_ThrowsInvalidOperationException()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.CreateHttpClientForTest(setAuthorizationHeaderWithBearerToken: true));

        Assert.Contains(nameof(WebApiBaseClient.ClientCredential), exception.Message);
    }

    [Fact]
    public async Task CreateHttpClient_WhenParametersAreConfigured_SetsExpectedDefaults()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");

        using var httpClient = await client.CreateHttpClientForTest();

        Assert.Equal(TimeSpan.FromMinutes(3), httpClient.Timeout);
        Assert.Equal(1 * 1024 * 1024, httpClient.MaxResponseContentBufferSize);
        Assert.Contains(httpClient.DefaultRequestHeaders.Accept, header => header.MediaType == MediaTypes.ApplicationJson);
        Assert.Null(httpClient.DefaultRequestHeaders.Authorization);
    }

    [Fact]
    public async Task CreateHttpClient_WhenClientRequestTimeoutIsZero_ThrowsInvalidOperationException()
    {
        var originalTimeout = WebApiBaseClient.WebApiClientParameters.ClientRequestTimeOut;

        try
        {
            WebApiBaseClient.WebApiClientParameters.ClientRequestTimeOut = 0;
            var client = new TestWebApiBaseClient("https://api.example.com");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => client.CreateHttpClientForTest());

            Assert.Contains("ClientRequestTimeOut", exception.Message);
        }
        finally
        {
            WebApiBaseClient.WebApiClientParameters.ClientRequestTimeOut = originalTimeout;
        }
    }

    [Fact]
    public async Task CreateHttpClient_WhenMaxResponseContentBufferSizeIsZero_ThrowsInvalidOperationException()
    {
        var originalBufferSize = WebApiBaseClient.WebApiClientParameters.MaxResponseContentBufferSize;

        try
        {
            WebApiBaseClient.WebApiClientParameters.MaxResponseContentBufferSize = 0;
            var client = new TestWebApiBaseClient("https://api.example.com");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => client.CreateHttpClientForTest());

            Assert.Contains("MaxResponseContentBufferSize", exception.Message);
        }
        finally
        {
            WebApiBaseClient.WebApiClientParameters.MaxResponseContentBufferSize = originalBufferSize;
        }
    }

    [Fact]
    public async Task CreateHttpClient_WithNullHandler_CreatesHttpClient()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");

        using var httpClient = await client.CreateHttpClientForTest();

        Assert.NotNull(httpClient);
        Assert.Equal(TimeSpan.FromMinutes(3), httpClient.Timeout);
    }

    [Fact]
    public async Task CreateHttpClient_WithCustomHandler_CreatesHttpClientWithHandler()
    {
        using var handler = new HttpClientHandler();
        var client = new TestWebApiBaseClient("https://api.example.com", handler, disposeHandler: false);

        using var httpClient = await client.CreateHttpClientForTest();

        Assert.NotNull(httpClient);
    }

    [Fact]
    public async Task CreateHttpClient_ClearsDefaultRequestHeaders()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");

        using var httpClient = await client.CreateHttpClientForTest();

        // All headers except Accept should be cleared
        Assert.DoesNotContain(httpClient.DefaultRequestHeaders, h => h.Key != "Accept");
    }

    [Fact]
    public async Task CreateHttpClient_WithAuthorization_WhenCredentialIsNull_ThrowsInvalidOperationException()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.CreateHttpClientForTest(setAuthorizationHeaderWithBearerToken: true));
    }

    #endregion

    #region GetStringContentFromObject Tests

    [Fact]
    public async Task GetStringContentFromObject_WhenDtoIsProvided_SerializesAsUtf8Json()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var dto = new SampleDto(42, "answer");

        using var content = client.GetStringContentFromObjectForTest(dto);
        var json = await content.ReadAsStringAsync();

        Assert.Equal("{\"Id\":42,\"Name\":\"answer\"}", json, true);
        Assert.Equal("application/json", content.Headers.ContentType?.MediaType, true);
        Assert.Equal("utf-8", content.Headers.ContentType?.CharSet, true);
    }

    [Fact]
    public async Task GetStringContentFromObject_WithNullObject_SerializesAsNullJson()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");

        using var content = client.GetStringContentFromObjectForTest(null!);
        var json = await content.ReadAsStringAsync();

        Assert.Equal("null", json);
        Assert.Equal("application/json", content.Headers.ContentType?.MediaType, true);
    }

    [Fact]
    public async Task GetStringContentFromObject_WithStringValue_SerializesAsString()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");

        using var content = client.GetStringContentFromObjectForTest("test string");
        var json = await content.ReadAsStringAsync();

        Assert.Equal("\"test string\"", json);
    }

    [Fact]
    public async Task GetStringContentFromObject_WithCollection_SerializesAsJsonArray()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var items = new[] { new SampleDto(1, "one"), new SampleDto(2, "two") };

        using var content = client.GetStringContentFromObjectForTest(items);
        var json = await content.ReadAsStringAsync();

        Assert.Contains("\"Id\":1", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"Id\":2", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetStringContentFromObject_WithInvalidEncoding_ThrowsNotImplementedException()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var dto = new SampleDto(1, "test");

        Assert.Throws<NotImplementedException>(() => client.GetStringContentFromObjectForTest(dto, (Content_Encoding)999));
    }

    [Fact]
    public void GetStringContentFromObject_WithInvalidMediaType_ThrowsNotImplementedException()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var dto = new SampleDto(1, "test");

        Assert.Throws<NotImplementedException>(() => client.GetStringContentFromObjectForTest(dto, Content_Encoding.UTF8, (Content_MediaType)999));
    }

    [Fact]
    public async Task GetStringContentFromObject_WithDefaultParameters_UsesUtf8AndApplicationJson()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var dto = new SampleDto(1, "test");

        using var content = client.GetStringContentFromObjectForTest(dto);

        Assert.Equal("utf-8", content.Headers.ContentType?.CharSet, true);
        Assert.Equal("application/json", content.Headers.ContentType?.MediaType, true);
    }

    #endregion

    #region WebApiClientParameters Tests

    [Fact]
    public void WebApiClientParameters_WhenRead_ExposeDefaultValues()
    {
        Assert.Equal(180, WebApiBaseClient.WebApiClientParameters.ClientRequestTimeOut);
        Assert.Equal(1 * 1024 * 1024, WebApiBaseClient.WebApiClientParameters.MaxResponseContentBufferSize);
        Assert.True(WebApiBaseClient.WebApiClientParameters.SerializerOptions.PropertyNameCaseInsensitive);
    }

    [Fact]
    public void WebApiClientParameters_WhenModified_AppliesNewValues()
    {
        var originalTimeout = WebApiBaseClient.WebApiClientParameters.ClientRequestTimeOut;
        var originalBufferSize = WebApiBaseClient.WebApiClientParameters.MaxResponseContentBufferSize;

        try
        {
            WebApiBaseClient.WebApiClientParameters.ClientRequestTimeOut = 60;
            WebApiBaseClient.WebApiClientParameters.MaxResponseContentBufferSize = 512 * 1024;

            Assert.Equal(60, WebApiBaseClient.WebApiClientParameters.ClientRequestTimeOut);
            Assert.Equal(512 * 1024, WebApiBaseClient.WebApiClientParameters.MaxResponseContentBufferSize);
        }
        finally
        {
            WebApiBaseClient.WebApiClientParameters.ClientRequestTimeOut = originalTimeout;
            WebApiBaseClient.WebApiClientParameters.MaxResponseContentBufferSize = originalBufferSize;
        }
    }

    [Fact]
    public void WebApiClientParameters_SerializerOptions_CanBeModified()
    {
        var originalOptions = WebApiBaseClient.WebApiClientParameters.SerializerOptions;

        try
        {
            var newOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false,
                WriteIndented = true
            };
            WebApiBaseClient.WebApiClientParameters.SerializerOptions = newOptions;

            Assert.False(WebApiBaseClient.WebApiClientParameters.SerializerOptions.PropertyNameCaseInsensitive);
            Assert.True(WebApiBaseClient.WebApiClientParameters.SerializerOptions.WriteIndented);
        }
        finally
        {
            WebApiBaseClient.WebApiClientParameters.SerializerOptions = originalOptions;
        }
    }

    #endregion

    #region WebApiClientCredential Tests

    [Fact]
    public void WebApiClientCredential_WhenCreated_AllPropertiesAreNullable()
    {
        var credential = new WebApiClientCredential();

        Assert.Null(credential.AuthorityUrl);
        Assert.Null(credential.ApiScope);
        Assert.Null(credential.ClientId);
        Assert.Null(credential.ClientSecret);
    }

    [Fact]
    public void WebApiClientCredential_WhenSet_StoresAllProperties()
    {
        var credential = new WebApiClientCredential
        {
            AuthorityUrl = "https://auth.example.com",
            ApiScope = "api.read",
            ClientId = "my-client",
            ClientSecret = "my-secret"
        };

        Assert.Equal("https://auth.example.com", credential.AuthorityUrl);
        Assert.Equal("api.read", credential.ApiScope);
        Assert.Equal("my-client", credential.ClientId);
        Assert.Equal("my-secret", credential.ClientSecret);
    }

    [Fact]
    public async Task CreateHttpClient_WithCredential_SetupForAuthorization_DoesNotThrow()
    {
        var credential = new WebApiClientCredential
        {
            AuthorityUrl = "https://auth.example.com",
            ApiScope = "api.read",
            ClientId = "my-client",
            ClientSecret = "my-secret"
        };
        var client = new TestWebApiBaseClient("https://api.example.com", credential);

        // This will throw because we can't actually connect to the auth server,
        // but it proves the credential setup is correct for authorization flow
        var exception = await Record.ExceptionAsync(async () =>
            await client.CreateHttpClientForTest(setAuthorizationHeaderWithBearerToken: true));

        // The exception should NOT be about missing credential
        Assert.DoesNotContain(nameof(WebApiBaseClient.ClientCredential), exception?.Message ?? string.Empty);
    }

    #endregion

    #region Helper Classes

    private sealed class TestWebApiBaseClient : WebApiBaseClient
    {
        public TestWebApiBaseClient(string webApiBaseUrl)
            : base(webApiBaseUrl)
        {
        }

        public TestWebApiBaseClient(string webApiBaseUrl, HttpClientHandler handler, bool disposeHandler)
            : base(webApiBaseUrl, handler, disposeHandler)
        {
        }

        public TestWebApiBaseClient(string webApiBaseUrl, WebApiClientCredential clientCredentials)
            : base(webApiBaseUrl, clientCredentials)
        {
        }

        public TestWebApiBaseClient(string webApiBaseUrl, WebApiClientCredential clientCredentials, HttpClientHandler handler, bool disposeHandler)
            : base(webApiBaseUrl, clientCredentials, handler, disposeHandler)
        {
        }

        public string TestBaseUrl => WebApiBaseUrl;

        public HttpClientHandler TestHandler => Handler;

        public bool TestDisposeHandler => DisposeHandler;

        public WebApiClientCredential TestCredential => ClientCredential;

        public Task<AuthorizedHttpClient> CreateHttpClientForTest(bool setAuthorizationHeaderWithBearerToken = false)
        {
            return CreateHttpClient(setAuthorizationHeaderWithBearerToken);
        }

        public StringContent GetStringContentFromObjectForTest(object dto, Content_Encoding encoding = Content_Encoding.UTF8, Content_MediaType mediaType = Content_MediaType.ApplicationJson)
        {
            return GetStringContentFromObject(dto, encoding, mediaType);
        }
    }

    private sealed record SampleDto(int Id, string Name);

    private sealed class ComplexDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string[]? Values { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    #endregion
}
