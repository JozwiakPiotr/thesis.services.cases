using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace DebtorApp.Services;

public sealed class KeycloakAccessTokenHandler(
    IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException(
                "An HTTP context is required to call the protected API.");
        var accessToken = await httpContext.GetTokenAsync("access_token");

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException(
                "The authenticated session does not contain an access token.");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
