using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Bitai.WebApi.Client;
using Bitai.WebApi.Common;

namespace Bitai.WebApi.Helpers.Tests;

public class WebApiBaseClientTests
{
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
    public async Task GetStringContentFromObject_WhenDtoIsProvided_SerializesAsUtf8Json()
    {
        var client = new TestWebApiBaseClient("https://api.example.com");
        var dto = new SampleDto(42, "answer");

        using var content = client.GetStringContentFromObjectForTest(dto);
        var json = await content.ReadAsStringAsync();

        Assert.Equal("{\"Id\":42,\"Name\":\"answer\"}", json);
        Assert.Equal("application/json", content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", content.Headers.ContentType?.CharSet);
    }

    [Fact]
    public void WebApiClientParameters_WhenRead_ExposeDefaultValues()
    {
        Assert.Equal(180, WebApiBaseClient.WebApiClientParameters.ClientRequestTimeOut);
        Assert.Equal(1 * 1024 * 1024, WebApiBaseClient.WebApiClientParameters.MaxResponseContentBufferSize);
        Assert.True(WebApiBaseClient.WebApiClientParameters.SerializerOptions.PropertyNameCaseInsensitive);
    }

    private sealed class TestWebApiBaseClient : WebApiBaseClient
    {
        public TestWebApiBaseClient(string webApiBaseUrl)
            : base(webApiBaseUrl)
        {
        }

        public Task<AuthorizedHttpClient> CreateHttpClientForTest(bool setAuthorizationHeaderWithBearerToken = false)
        {
            return CreateHttpClient(setAuthorizationHeaderWithBearerToken);
        }

        public StringContent GetStringContentFromObjectForTest(object dto)
        {
            return GetStringContentFromObject(dto);
        }
    }

    private sealed record SampleDto(int Id, string Name);
}
