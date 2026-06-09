using Bitai.WebApi.Server;

namespace Bitai.WebApi.Helpers.Tests;

public class MiddlewareExceptionModelTests
{
    [Fact]
    public void Constructor_WhenDefaultConstructorIsUsed_InitializesEmptyDetails()
    {
        var model = new MiddlewareExceptionModel();

        Assert.True(model.IsMiddlewareException);
        Assert.NotNull(model.ErrorDetail);
        Assert.Empty(model.ErrorDetail);
    }

    [Fact]
    public void Constructor_WhenExceptionIsProvided_MapsExceptionPropertiesAndData()
    {
        var exception = new InvalidOperationException("Top level failure");
        exception.Data["Code"] = 409;
        exception.Data["MissingValue"] = null;

        var model = new MiddlewareExceptionModel(exception);

        Assert.True(model.IsMiddlewareException);
        Assert.Equal(typeof(InvalidOperationException).FullName, model.Type);
        Assert.Equal("Top level failure", model.Message);
        Assert.Equal(exception.Source, model.Source);
        Assert.Equal(exception.StackTrace, model.StackTrace);
        Assert.Contains("Code: 409", model.ErrorDetail);
        Assert.Contains("MissingValue: null", model.ErrorDetail);
        Assert.Null(model.InnerMiddlewareException);
    }

    [Fact]
    public void Constructor_WhenExceptionHasInnerException_MapsInnerExceptionRecursively()
    {
        var innerMost = new ArgumentException("Inner most failure");
        var inner = new ApplicationException("Inner failure", innerMost);
        var outer = new InvalidOperationException("Outer failure", inner);

        var model = new MiddlewareExceptionModel(outer);

        Assert.Equal(typeof(InvalidOperationException).FullName, model.Type);
        Assert.NotNull(model.InnerMiddlewareException);
        Assert.Equal(typeof(ApplicationException).FullName, model.InnerMiddlewareException.Type);
        Assert.NotNull(model.InnerMiddlewareException.InnerMiddlewareException);
        Assert.Equal(typeof(ArgumentException).FullName, model.InnerMiddlewareException.InnerMiddlewareException.Type);
    }

    [Fact]
    public void ToString_WhenCalled_ReturnsTypeAndMessage()
    {
        var model = new MiddlewareExceptionModel
        {
            Type = "CustomException",
            Message = "Custom message"
        };

        var result = model.ToString();

        Assert.Equal("CustomException: Custom message", result);
    }

    [Fact]
    public void ToStringReport_WhenStackTraceAndInnerErrorsAreIncluded_ContainsNestedReports()
    {
        var model = new MiddlewareExceptionModel
        {
            Type = "OuterException",
            Message = "Outer message",
            Source = "OuterSource",
            StackTrace = "Outer stack",
            ErrorDetail = new[] { "Outer detail" },
            InnerMiddlewareException = new MiddlewareExceptionModel
            {
                Type = "InnerException",
                Message = "Inner message",
                Source = "InnerSource",
                StackTrace = "Inner stack",
                ErrorDetail = new[] { "Inner detail" }
            }
        };

        var report = model.ToStringReport(includeStackTrace: true, includeInnerErrors: true);

        Assert.Contains("Description: OuterException: Outer message", report);
        Assert.Contains("Source: OuterSource", report);
        Assert.Contains("Details: Outer detail", report);
        Assert.Contains("Stack Trace: Outer stack", report);
        Assert.Contains("Inner Exception:", report);
        Assert.Contains("Description: InnerException: Inner message", report);
        Assert.Contains("Stack Trace: Inner stack", report);
    }

    [Fact]
    public void ToStringReport_WhenStackTraceIsExcluded_DoesNotContainStackTrace()
    {
        var model = new MiddlewareExceptionModel
        {
            Type = "OuterException",
            Message = "Outer message",
            Source = "OuterSource",
            StackTrace = "Outer stack",
            ErrorDetail = new[] { "Outer detail" }
        };

        var report = model.ToStringReport(includeStackTrace: false, includeInnerErrors: true);

        Assert.DoesNotContain("Stack Trace:", report);
        Assert.DoesNotContain("Outer stack", report);
    }

    [Fact]
    public void ToStringReport_WhenInnerErrorsAreExcluded_ReportsThatInnerErrorExists()
    {
        var model = new MiddlewareExceptionModel
        {
            Type = "OuterException",
            Message = "Outer message",
            Source = "OuterSource",
            ErrorDetail = Array.Empty<string>(),
            InnerMiddlewareException = new MiddlewareExceptionModel
            {
                Type = "InnerException",
                Message = "Inner message",
                ErrorDetail = Array.Empty<string>()
            }
        };

        var report = model.ToStringReport(includeStackTrace: false, includeInnerErrors: false);

        Assert.Contains("Inner Exception: Si, existe error anidado.", report);
        Assert.DoesNotContain("Description: InnerException", report);
    }

    [Fact]
    public void ToStringReport_WhenCalledWithoutArguments_IncludesStackTraceAndInnerErrors()
    {
        var model = new MiddlewareExceptionModel
        {
            Type = "OuterException",
            Message = "Outer message",
            Source = "OuterSource",
            StackTrace = "Outer stack",
            ErrorDetail = Array.Empty<string>(),
            InnerMiddlewareException = new MiddlewareExceptionModel
            {
                Type = "InnerException",
                Message = "Inner message",
                Source = "InnerSource",
                StackTrace = "Inner stack",
                ErrorDetail = Array.Empty<string>()
            }
        };

        var report = model.ToStringReport();

        Assert.Contains("Stack Trace: Outer stack", report);
        Assert.Contains("Description: InnerException: Inner message", report);
    }

    [Fact]
    public void ToHtmlReport_WhenCalled_ThrowsNotImplementedException()
    {
        var model = new MiddlewareExceptionModel();

        Assert.Throws<NotImplementedException>(() => model.ToHtmlReport());
        Assert.Throws<NotImplementedException>(() => model.ToHtmlReport(includeStackTrace: true, includeInnerErrors: true));
    }
}
