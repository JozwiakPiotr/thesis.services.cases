using Microsoft.EntityFrameworkCore;
using PaymentsApp.ApiService.Features.Accounts;
using PaymentsApp.ApiService.Infrastructure.Persistence;
using AccountEndpoints = PaymentsApp.ApiService.Features.Accounts.Endpoint;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<PaymentsDbContext>("payments");
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApi();
builder.Services.AddValidation();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Payments API is running.");
AccountEndpoints.Map(app);
app.MapDefaultEndpoints();

app.Run();
