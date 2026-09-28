public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAtUtc);

public sealed record CurrentUserResponse(string Username);
