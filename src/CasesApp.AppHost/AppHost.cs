var builder = DistributedApplication.CreateBuilder(args);

var kubernetes = builder.AddKubernetesEnvironment("k8s")
    .WithHelm(helm =>
    {
        helm.WithChartName("cases-app")
            .WithChartVersion("1.0.0")
            .WithChartDescription("Cases and payments services")
            .WithReleaseName("cases-app")
            .WithNamespace("cases");
    });

var postgres = builder.AddPostgres("postgres");
var casesDatabase = postgres.AddDatabase("cases");
var paymentsDatabase = postgres.AddDatabase("payments");

var apiService = builder.AddProject<Projects.CasesApp_ApiService>("apiservice")
    .WithReference(casesDatabase)
    .WithEnvironment("Keycloak__Authority", "http://192.168.102.1:8080/realms/cases")
    .WithComputeEnvironment(kubernetes)
    .WaitFor(casesDatabase)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.DebtorApp>("debtorapp")
    .WithReference(apiService)
    .WithEnvironment("Keycloak__Authority", "http://192.168.102.1:8080/realms/cases")
    .WithEndpoint("https", endpoint => endpoint.Port = 7338)
    .WithEndpoint("http", endpoint => endpoint.Port = 5590)
    .WithComputeEnvironment(kubernetes)
    .WaitFor(apiService)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.PaymentsApp_ApiService>("paymentsapi")
    .WithReference(paymentsDatabase)
    .WithEnvironment("Keycloak__Authority", "http://192.168.102.1:8080/realms/cases")
    .WithComputeEnvironment(kubernetes)
    .WaitFor(paymentsDatabase)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();
