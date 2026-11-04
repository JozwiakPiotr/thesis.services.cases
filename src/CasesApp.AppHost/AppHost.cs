var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var casesDatabase = postgres.AddDatabase("cases");
var paymentsDatabase = postgres.AddDatabase("payments");

var apiService = builder.AddProject<Projects.CasesApp_ApiService>("apiservice")
    .WithReference(casesDatabase)
    .WaitFor(casesDatabase)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.DebtorApp>("debtorapp")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.PaymentsApp_ApiService>("paymentsapi")
    .WithReference(paymentsDatabase)
    .WaitFor(paymentsDatabase)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
