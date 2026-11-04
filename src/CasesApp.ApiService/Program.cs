using CasesApp.ApiService.Features.DebtCases;
using CasesApp.ApiService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<DebtCaseDbContext>("cases");

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DebtCaseDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Cases API is running.");
app.MapDebtCaseEndpoints();

app.MapDefaultEndpoints();

app.Run();
