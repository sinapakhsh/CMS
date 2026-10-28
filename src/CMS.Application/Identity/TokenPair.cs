namespace CMS.Application.Identity;

// Tokens are returned only after their session has committed successfully.
public sealed record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAtUtc);
