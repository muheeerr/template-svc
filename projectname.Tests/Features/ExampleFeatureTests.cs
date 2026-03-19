using System.Net.Http.Headers;
using FluentAssertions;
using projectname.Tests.Common;

namespace projectname.Tests.Features;

public class ExampleFeatureTests(TestWebAppFactory factory)
    : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetExample_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/v1/Example/GetExample");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetExample_WithValidToken_Returns200()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuthHelper.GenerateTestJwt());

        var response = await _client.GetAsync("/v1/Example/GetExample");
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}

