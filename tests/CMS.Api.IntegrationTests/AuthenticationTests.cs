using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using CMS.Application.Identity;
using CMS.SharedKernel.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace CMS.Api.IntegrationTests;

// The real JwtBearer middleware validates signature, lifetime, audience, revocation and permissions.
public sealed class AuthenticationTests
{
    private const string _key = "test-only-key-never-use-in-production-123456789";

    // Spoofed headers cannot select a tenant or bypass authentication.
    [Fact]
    public async Task Anonymous_or_forged_requests_are_rejected()
    {
        using var factory = Factory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/platform/locales/")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("wrong-audience"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/identity/logout", null)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("cms-api", expired: true));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/identity/logout", null)).StatusCode);
    }

    // A valid member token is authenticated but cannot administer the tenant.
    [Fact]
    public async Task Member_is_forbidden_and_revoked_session_is_unauthorized()
    {
        using var factory = Factory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("cms-api"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/platform/locales/")).StatusCode);
        factory.Services.GetRequiredService<FakeIdentity>().Active = false;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/identity/logout", null)).StatusCode);
    }

    // Credential throttling emits a retry hint and business errors retain their stable code.
    [Fact]
    public async Task Login_errors_are_problem_details_and_requests_are_rate_limited()
    {
        using var factory = Factory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/identity/login", new { tenant = "test", email = "x", password = "x" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Identity.InvalidCredentials", await response.Content.ReadAsStringAsync());
        for (var i = 0; i < 9; i++)
            using (await client.PostAsJsonAsync("/api/v1/identity/login", new { tenant = "test", email = "x", password = "x" })) { }
        using var limited = await client.PostAsJsonAsync("/api/v1/identity/login", new { tenant = "test", email = "x", password = "x" });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.NotNull(limited.Headers.RetryAfter);
    }

    private static WebApplicationFactory<Program> Factory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Cms"] = "Server=localhost,1;Database=CMS_Test_Unavailable;Integrated Security=true;TrustServerCertificate=true;Connect Timeout=1",
            ["Outbox:Enabled"] = "false",
            ["Jwt:SigningKey"] = _key
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIdentityService>();
            services.AddSingleton<FakeIdentity>();
            services.AddSingleton<IIdentityService>(provider => provider.GetRequiredService<FakeIdentity>());
        });
    });

    private static string Token(string audience, bool expired = false)
    {
        var now = DateTime.UtcNow;
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("cms", audience,
            [new Claim("tenant_id", Guid.NewGuid().ToString()), new Claim("sub", Guid.NewGuid().ToString()), new Claim("sid", Guid.NewGuid().ToString())],
            now.AddHours(-1), expired ? now.AddMinutes(-1) : now.AddMinutes(5),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)), SecurityAlgorithms.HmacSha256)));
    }

    private sealed class FakeIdentity : IIdentityService
    {
        public bool Active { get; set; } = true;
        public Task<IReadOnlyList<string>> GetPermissionsAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<bool> IsSessionActiveAsync(Guid tenantId, Guid userId, Guid sessionId, CancellationToken cancellationToken) => Task.FromResult(Active);
        public Task<Result<TokenPair>> LoginAsync(string tenant, string email, string password, CancellationToken cancellationToken) =>
            Task.FromResult(Result<TokenPair>.Failure(new Error("Identity.InvalidCredentials", "Invalid credentials.")));
        public Task<Result<Guid>> RegisterAsync(string tenant, string email, string password, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<TokenPair>> RefreshAsync(string tenant, string token, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<Guid>> LogoutAsync(Guid sessionId, CancellationToken cancellationToken) => Task.FromResult(Result<Guid>.Success(sessionId));
    }
}
