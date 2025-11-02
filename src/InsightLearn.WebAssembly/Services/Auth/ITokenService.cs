namespace InsightLearn.WebAssembly.Services.Auth;

public interface ITokenService
{
    Task<string?> GetTokenAsync();
    Task SetTokenAsync(string token);
    Task<string?> GetRefreshTokenAsync();
    Task SetRefreshTokenAsync(string refreshToken);
    Task ClearTokensAsync();
    Task<bool> IsTokenValidAsync();
}
