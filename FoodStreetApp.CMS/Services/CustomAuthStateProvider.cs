using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace FoodStreetApp.CMS.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;

    public CustomAuthStateProvider(IJSRuntime jsRuntime, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string? token = null;
        try
        {
            token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "token");
        }
        catch
        {
            // JS Interop not available yet (e.g. pre-rendering)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt", "unique_name", "role");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyUserAuthentication(string token)
    {
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt", "unique_name", "role");
        var authenticatedUser = new ClaimsPrincipal(identity);
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        NotifyAuthenticationStateChanged(authState);
    }

    public void NotifyUserLogout()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            foreach (var kvp in keyValuePairs)
            {
                var value = kvp.Value.ToString();
                if (value == null) continue;

                // Handle array roles if present
                if (value.Trim().StartsWith("["))
                {
                    var values = JsonSerializer.Deserialize<string[]>(value);
                    if (values != null)
                    {
                        foreach (var val in values) claims.Add(new Claim(kvp.Key, val));
                    }
                }
                else
                {
                    claims.Add(new Claim(kvp.Key, value));
                }

                // Map common JWT claims to standard claim types for better compatibility
                if (kvp.Key == "role" || kvp.Key == "roles") 
                    claims.Add(new Claim(ClaimTypes.Role, value));
                if (kvp.Key == "unique_name" || kvp.Key == "sub")
                    claims.Add(new Claim(ClaimTypes.Name, value));
            }
        }
        return claims;
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
