using CasesApp.ApiService.Domain;
using CasesApp.ApiService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CasesApp.ApiService.Features.DebtCases.Activate;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/api/cases/commands/activate",
                async Task<Results<NoContent, NotFound, Conflict<string>>> (
                    Request request,
                    DebtCaseDbContext dbContext,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    var debtCase = await dbContext.DebtCases
                        .SingleOrDefaultAsync(
                            item => item.CaseNumber == request.CaseNumber,
                            cancellationToken);

                    if (debtCase is null)
                    {
                        return TypedResults.NotFound();
                    }

                    if (debtCase.LifecycleStatus != DebtCaseLifecycleStatus.Draft)
                    {
                        return TypedResults.Conflict(
                            $"A case in status '{debtCase.LifecycleStatus}' cannot be activated.");
                    }

                    debtCase.Activate(timeProvider.GetUtcNow());
                    await dbContext.SaveChangesAsync(cancellationToken);

                    return TypedResults.NoContent();
                })
            .WithName("ActivateCase")
            .WithSummary("Activates a debt case")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<string>(StatusCodes.Status409Conflict);
    }
}

public sealed record Request
{
    public required string CaseNumber { get; init; }
}
