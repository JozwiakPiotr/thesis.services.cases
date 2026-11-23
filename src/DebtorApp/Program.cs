using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Net;
using DebtorApp.Components;
using DebtorApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpContextAccessor();
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"]
            ?? throw new InvalidOperationException("Keycloak:Authority configuration is required.");
        var metadataAddress = builder.Configuration["Keycloak:MetadataAddress"]
            ?? throw new InvalidOperationException("Keycloak:MetadataAddress configuration is required.");
        options.MetadataAddress = metadataAddress;
        options.ClientId = builder.Configuration["Keycloak:ClientId"] ?? "debtorapp";
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.RequireHttpsMetadata = false;
        var publicBaseAddress = new Uri(options.Authority);
        var backchannelBaseAddress = new Uri(
            builder.Configuration["Keycloak:BackchannelBaseAddress"]
                ?? throw new InvalidOperationException(
                    "Keycloak:BackchannelBaseAddress configuration is required."));
        var backchannel = new HttpClient(
            new KeycloakBackchannelHandler(publicBaseAddress, backchannelBaseAddress))
        {
            BaseAddress = backchannelBaseAddress,
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
        };
        options.Backchannel = backchannel;
        options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever(backchannel)
            {
                RequireHttps = false
            });
        options.GetClaimsFromUserInfoEndpoint = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddTransient<KeycloakAccessTokenHandler>();
builder.Services.AddHttpClient("CasesApi", client =>
{
    var baseAddress = builder.Configuration["CasesApi:BaseAddress"]
        ?? throw new InvalidOperationException("CasesApi:BaseAddress configuration is required.");
    client.BaseAddress = new Uri(baseAddress);
}).AddHttpMessageHandler<KeycloakAccessTokenHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/authentication/login", (HttpContext context, string? returnUrl) =>
{
    var redirectUri = string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/')
        ? "/"
        : returnUrl;

    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = redirectUri },
        [OpenIdConnectDefaults.AuthenticationScheme]);
}).AllowAnonymous();

app.MapGet("/authentication/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
}).AllowAnonymous();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();
app.MapDefaultEndpoints();

app.Run();

sealed class KeycloakBackchannelHandler(
    Uri publicBaseAddress,
    Uri backchannelBaseAddress) : DelegatingHandler(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    })
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri is Uri uri
            && uri.Host.Equals(
                publicBaseAddress.Host,
                StringComparison.OrdinalIgnoreCase))
        {
            request.RequestUri = new UriBuilder(uri)
            {
                Scheme = backchannelBaseAddress.Scheme,
                Host = backchannelBaseAddress.Host,
                Port = backchannelBaseAddress.IsDefaultPort
                    ? -1
                    : backchannelBaseAddress.Port
            }.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
