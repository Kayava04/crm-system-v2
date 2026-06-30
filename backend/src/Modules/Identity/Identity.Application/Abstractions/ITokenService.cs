namespace Identity.Application.Abstractions;

public interface ITokenService
{
    string GenerateAccessToken(
        Guid userId,
        string email,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions
    );

    string GenerateRefreshToken();
    string HashToken(string token);
}
