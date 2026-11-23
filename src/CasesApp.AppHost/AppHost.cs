var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var casesDatabase = postgres.AddDatabase("cases");
var paymentsDatabase = postgres.AddDatabase("payments");

var apiService = builder.AddProject<Projects.CasesApp_ApiService>("apiservice")
    .WithReference(casesDatabase)
    .WithEnvironment("Keycloak__Authority", "https://keycloak.contoso.lab/realms/cases")
    .WithEnvironment("Keycloak__MetadataAddress", "http://keycloak.cases.svc.cluster.local:8080/realms/cases/.well-known/openid-configuration")
    .WaitFor(casesDatabase)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.DebtorApp>("debtorapp")
    .WithReference(apiService)
    .WithEnvironment("Keycloak__Authority", "https://keycloak.contoso.lab/realms/cases")
    .WithEnvironment("Keycloak__MetadataAddress", "http://keycloak.cases.svc.cluster.local:8080/realms/cases/.well-known/openid-configuration")
    .WithEnvironment("Keycloak__BackchannelBaseAddress", "http://keycloak.cases.svc.cluster.local:8080")
    .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true")
    .WithEndpoint("https", endpoint => endpoint.Port = 7338)
    .WithEndpoint("http", endpoint => endpoint.Port = 5590)
    .WaitFor(apiService)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.PaymentsApp_ApiService>("paymentsapi")
    .WithReference(paymentsDatabase)
    .WithEnvironment("Keycloak__Authority", "https://keycloak.contoso.lab/realms/cases")
    .WaitFor(paymentsDatabase)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();
