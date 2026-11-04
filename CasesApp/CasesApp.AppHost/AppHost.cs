var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var casesDatabase = postgres.AddDatabase("cases");

var apiService = builder.AddProject<Projects.CasesApp_ApiService>("apiservice")
    .WithReference(casesDatabase)
    .WaitFor(casesDatabase)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
