using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
        options.ClientId = builder.Configuration["Keycloak:ClientId"] ?? "debtorapp";
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.RequireHttpsMetadata = false;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        options.Backchannel = new HttpClient(handler)
        {
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
        };
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
    client.BaseAddress = new Uri("http://apiservice");
}).AddHttpMessageHandler<KeycloakAccessTokenHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

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
