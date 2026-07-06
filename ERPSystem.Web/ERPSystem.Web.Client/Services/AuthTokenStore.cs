namespace ERPSystem.Web.Client.Services;

public sealed class AuthTokenStore
{
    public string? AccessToken { get; private set; }

    public void SetAccessToken(string token) => AccessToken = token;

    public void Clear() => AccessToken = null;
}
