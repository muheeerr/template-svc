using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using projectname.Tests.Common;

namespace projectname.Tests.Features;

public class ExampleFeatureTests(TestWebAppFactory factory)
    : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetExample_WithoutToken_ReturnsOk()
    {
        // Example endpoint does not require authorization (template default)
        var response = await _client.GetAsync("/v1/Example/GetExample");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetExample_WithValidToken_Returns200()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuthHelper.GenerateTestJwt());

        var response = await _client.GetAsync("/v1/Example/GetExample");
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task GetExample_Response_ContainsCorrelationIdHeader()
    {
        var response = await _client.GetAsync("/v1/Example/GetExample");
        response.Headers.Should().ContainKey("X-Correlation-ID");
        response.Headers.GetValues("X-Correlation-ID").First().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetExample_Response_ContainsSecurityHeaders()
    {
        var response = await _client.GetAsync("/v1/Example/GetExample");
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");
    }

    [Fact]
    public async Task CreateExample_WithValidBody_Returns201()
    {
        var body = new { Name = "Test Item", Description = "A test description" };
        var response = await _client.PostAsJsonAsync("/v1/Example/CreateExample", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        json.GetProperty("data").GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateExample_ReturnsCreatedWithResponseShape()
    {
        var body = new { Name = "Shape Test", Description = "Testing response shape" };
        var response = await _client.PostAsJsonAsync("/v1/Example/CreateExample", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);

        // Verify API response envelope structure
        json.TryGetProperty("data", out _).Should().BeTrue();
        json.TryGetProperty("message", out _).Should().BeTrue();
        json.TryGetProperty("statusCode", out _).Should().BeTrue();
    }
}

