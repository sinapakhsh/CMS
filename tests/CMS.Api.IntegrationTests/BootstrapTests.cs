using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CMS.Api.IntegrationTests;

// Verify actual application startup and documentation exposure in both environments.
public sealed class BootstrapTests
{
    // A single OpenAPI generator should produce a valid document in development.
    [Fact]
    public async Task Development_exposes_openapi_and_scalar()
    {
        using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.StartsWith("3.", document.RootElement.GetProperty("openapi").GetString());
        Assert.True(document.RootElement.TryGetProperty("paths", out _));

        using var scalar = await client.GetAsync("/scalar/v1");
        Assert.Equal(HttpStatusCode.OK, scalar.StatusCode);
        Assert.Equal("text/html", scalar.Content.Headers.ContentType?.MediaType);
    }

    // Production must not expose development documentation or deleted feature endpoints.
    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar/v1")]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/weatherforecast")]
    [InlineData("/api/v1/health")]
    public async Task Production_returns_problem_details_for_unmapped_routes(string path)
    {
        using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Http.404", document.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("traceId").GetString()));
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Cms"] = "Server=localhost,1;Database=CMS_Test_Unavailable;Integrated Security=true;TrustServerCertificate=true;Connect Timeout=1",
                ["Outbox:Enabled"] = "false"
                ,
                ["Jwt:SigningKey"] = "test-only-key-never-use-in-production-123456789"
            }));
        });
}
