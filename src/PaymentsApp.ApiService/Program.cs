using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using PaymentsApp.ApiService.Features.Accounts;
using PaymentsApp.ApiService.Infrastructure.Persistence;
using AccountEndpoints = PaymentsApp.ApiService.Features.Accounts.Endpoint;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<PaymentsDbContext>("payments");
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var authority = builder.Configuration["Keycloak:Authority"]
            ?? throw new InvalidOperationException("Keycloak:Authority configuration is required.");
        var metadataAddress = builder.Configuration["Keycloak:MetadataAddress"]
            ?? throw new InvalidOperationException("Keycloak:MetadataAddress configuration is required.");
        var backchannelBaseAddress = builder.Configuration["Keycloak:BackchannelBaseAddress"]
            ?? throw new InvalidOperationException(
                "Keycloak:BackchannelBaseAddress configuration is required.");

        options.Authority = authority;
        options.MetadataAddress = metadataAddress;
        options.Audience = builder.Configuration["Keycloak:Audience"] ?? "api";
        options.RequireHttpsMetadata = false;

        var backchannel = new HttpClient(
            new KeycloakBackchannelHandler(
                new Uri(authority),
                new Uri(backchannelBaseAddress)))
        {
            BaseAddress = new Uri(backchannelBaseAddress)
        };
        options.Backchannel = backchannel;
        options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever(backchannel)
            {
                RequireHttps = false
            });
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddOpenApi();
builder.Services.AddValidation();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Payments API is running.");
AccountEndpoints.Map(app);
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
