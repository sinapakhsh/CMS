using CMS.Application.Identity;
using CMS.Application.Platform.Tenants;
using CMS.Domain.Identity.Sessions;
using CMS.Domain.Identity.Users;
using CMS.Infrastructure.Identity;
using CMS.Infrastructure.Persistence.Write;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.IntegrationTests;

// Exercise real password hashing, rotation, revocation and tenant isolation through SQL.
public sealed class IdentityPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    // Simultaneous token consumption has one winner and the replay revokes that entire family.
    [Fact]
    public async Task Concurrent_refresh_detects_reuse()
    {
        var tenantId = Guid.NewGuid();
        var slug = "refresh-" + tenantId.ToString("N");
        await using var setup = database.Scope(tenantId);
        Assert.True((await setup.ServiceProvider.GetRequiredService<ISender>().Send(new CreateTenantCommand(slug, "Test", "setup"))).IsSuccess);
        setup.ServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value.SigningKey = "test-only-cryptographic-signing-key-1234567890";
        var identity = setup.ServiceProvider.GetRequiredService<IIdentityService>();
        var user = await identity.RegisterAsync(slug, "race@example.test", "Long-test-password!", default);
        var login = await identity.LoginAsync(slug, "race@example.test", "Long-test-password!", default);
        async Task<bool> RefreshAsync()
        {
            await using var scope = database.Scope(tenantId);
            return (await scope.ServiceProvider.GetRequiredService<IIdentityService>().RefreshAsync(slug, login.Value.RefreshToken, default)).IsSuccess;
        }
        var outcomes = await Task.WhenAll(RefreshAsync(), RefreshAsync());
        Assert.Single(outcomes, success => success);
        Assert.False(await identity.IsSessionActiveAsync(tenantId, user.Value,
            Guid.ParseExact(login.Value.RefreshToken[..32], "N"), default));
    }

    // A consumed refresh token invalidates the newly issued access-token family.
    [Fact]
    public async Task Registration_login_rotation_replay_and_tenant_isolation()
    {
        var tenantId = Guid.NewGuid();
        var slug = "identity-" + tenantId.ToString("N");
        await using var scope = database.Scope(tenantId);
        Assert.True((await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CreateTenantCommand(slug, "Identity", "setup"))).IsSuccess);
        scope.ServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value.SigningKey = "test-only-cryptographic-signing-key-1234567890";
        var service = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var registration = await service.RegisterAsync(slug, "person@example.test", "Long-test-password!", default);
        Assert.True(registration.IsSuccess);
        Assert.True((await service.RegisterAsync(slug, "PERSON@example.test", "Long-test-password!", default)).IsFailure);
        Assert.True((await service.LoginAsync(slug, "person@example.test", "wrong", default)).IsFailure);
        var login = await service.LoginAsync(slug, "person@example.test", "Long-test-password!", default);
        Assert.True(login.IsSuccess);
        var sessionId = Guid.ParseExact(login.Value.RefreshToken[..32], "N");
        Assert.True(await service.IsSessionActiveAsync(tenantId, registration.Value, sessionId, default));
        Assert.False(await service.IsSessionActiveAsync(Guid.NewGuid(), registration.Value, sessionId, default));
        Assert.True((await service.RefreshAsync(slug, sessionId.ToString("N") + ".invalid", default)).IsFailure);
        Assert.True(await service.IsSessionActiveAsync(tenantId, registration.Value, sessionId, default));
        var rotated = await service.RefreshAsync(slug, login.Value.RefreshToken, default);
        Assert.True(rotated.IsSuccess);
        Assert.NotEqual(login.Value.RefreshToken, rotated.Value.RefreshToken);
        Assert.True((await service.RefreshAsync(slug, login.Value.RefreshToken, default)).IsFailure);
        Assert.False(await service.IsSessionActiveAsync(tenantId, registration.Value, sessionId, default));
        Assert.True((await service.RefreshAsync(slug, rotated.Value.RefreshToken, default)).IsFailure);
        var context = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
        var user = await context.Set<User>().SingleAsync();
        Assert.NotEqual("Long-test-password!", user.PasswordHash);
        Assert.False(user.IsAdministrator);
        var session = await context.Set<LoginSession>().SingleAsync();
        Assert.DoesNotContain(login.Value.RefreshToken, session.ConsumedHashesJson);
        Assert.NotEqual(rotated.Value.RefreshToken, session.RefreshHash);
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var role = await sender.Send(new CreateRoleCommand("Editor", ["content.edit"], "role"));
        Assert.True(role.IsSuccess);
        Assert.True((await sender.Send(new AssignRoleCommand(user.Id, role.Value, "assign"))).IsSuccess);
        Assert.Equal(new[] { "content.edit" }, await service.GetPermissionsAsync(tenantId, user.Id, default));
        Assert.Empty(await service.GetPermissionsAsync(Guid.NewGuid(), user.Id, default));
    }

    // Failed logins persist the lockout and logout immediately invalidates a valid family.
    [Fact]
    public async Task Lockout_and_logout_are_durable()
    {
        var tenantId = Guid.NewGuid();
        var slug = "lockout-" + tenantId.ToString("N");
        await using var scope = database.Scope(tenantId);
        Assert.True((await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CreateTenantCommand(slug, "Test", "setup"))).IsSuccess);
        scope.ServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value.SigningKey = "test-only-cryptographic-signing-key-1234567890";
        var service = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var user = await service.RegisterAsync(slug, "lock@example.test", "Long-test-password!", default);
        for (var attempt = 0; attempt < 5; attempt++)
            Assert.True((await service.LoginAsync(slug, "lock@example.test", "incorrect", default)).IsFailure);
        Assert.True((await service.LoginAsync(slug, "lock@example.test", "Long-test-password!", default)).IsFailure);
        database.Clock.UtcNow = database.Clock.UtcNow.AddMinutes(16);
        var login = await service.LoginAsync(slug, "lock@example.test", "Long-test-password!", default);
        Assert.True(login.IsSuccess);
        var sessionId = Guid.ParseExact(login.Value.RefreshToken[..32], "N");
        scope.ServiceProvider.GetRequiredService<TestIdentity>().UserId = user.Value;
        Assert.True((await service.LogoutAsync(sessionId, default)).IsSuccess);
        Assert.False(await service.IsSessionActiveAsync(tenantId, user.Value, sessionId, default));
    }
}
