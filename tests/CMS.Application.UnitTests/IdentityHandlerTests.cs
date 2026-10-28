using CMS.Application.Identity;
using CMS.SharedKernel.Results;
using NSubstitute;

namespace CMS.Application.UnitTests;

// Identity handlers must preserve failure results and forward cancellation without logging secrets.
public sealed class IdentityHandlerTests
{
    // Successful and rejected credential results use the same orchestration path.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handlers_preserve_service_results(bool success)
    {
        var service = Substitute.For<IIdentityService>();
        var id = Guid.NewGuid();
        var result = success ? Result<Guid>.Success(id) : Result<Guid>.Failure(new Error("Identity.Invalid", "Invalid"));
        var pair = success ? Result<TokenPair>.Success(new TokenPair("access", "refresh", DateTimeOffset.UtcNow))
            : Result<TokenPair>.Failure(new Error("Identity.Invalid", "Invalid"));
        using var cancellation = new CancellationTokenSource();
        var ct = cancellation.Token;
        service.RegisterAsync("tenant", "email", "password", ct).Returns(result);
        service.LoginAsync("tenant", "email", "password", ct).Returns(pair);
        service.RefreshAsync("tenant", "refresh", ct).Returns(pair);
        service.LogoutAsync(id, ct).Returns(result);
        Assert.Same(result, await new RegisterHandler(service).Handle(new RegisterCommand("tenant", "email", "password"), ct));
        Assert.Same(pair, await new LoginHandler(service).Handle(new LoginCommand("tenant", "email", "password"), ct));
        Assert.Same(pair, await new RefreshHandler(service).Handle(new RefreshCommand("tenant", "refresh"), ct));
        Assert.Same(result, await new LogoutHandler(service).Handle(new LogoutCommand(id), ct));
    }
}
