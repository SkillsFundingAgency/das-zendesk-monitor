using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using ZenWatchFunction;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Xunit;

public class HttpRequestDataExtensionsTests
{
    private readonly Mock<HttpRequestData> _mockRequest;
    private readonly Mock<FunctionContext> _mockContext;

    public HttpRequestDataExtensionsTests()
    {
        _mockContext = new Mock<FunctionContext>();
        _mockRequest = new Mock<HttpRequestData>(_mockContext.Object);
    }

    private void SetupRequestBody(string json)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        _mockRequest.Setup(r => r.Body).Returns(stream);
    }

    [Fact]
    public async Task GetJsonBody_ValidJsonAndValidModel_ReturnsSuccess()
    {
        var json = JsonSerializer.Serialize(new TestModel { Name = "Valid" });
        SetupRequestBody(json);

        var result = await _mockRequest.Object.GetJsonBody<TestModel, TestModelValidator>();

        result.IsSuccess.Should().BeTrue();
        result.IfSucc(model => model.Name.Should().Be("Valid"));
    }

    [Fact]
    public async Task GetJsonBody_ValidJsonButValidationFails_ReturnsFailure()
    {
        var json = JsonSerializer.Serialize(new TestModel { Name = "" }); 
        SetupRequestBody(json);

        var result = await _mockRequest.Object.GetJsonBody<TestModel, TestModelValidator>();

        result.IsFaulted.Should().BeTrue();
        result.IfFail(ex => ex.Message.Should().Contain("Name"));
    }

    [Fact]
    public async Task GetJsonBody_NullModel_ReturnsFailure()
    {
        SetupRequestBody("null");

        var result = await _mockRequest.Object.GetJsonBody<TestModel, TestModelValidator>();

        result.IsFaulted.Should().BeTrue();
        result.IfFail(ex => ex.Message.Should().Be("Deserialized model is null."));
    }
}
