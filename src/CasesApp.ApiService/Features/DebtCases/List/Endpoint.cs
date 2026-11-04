using CasesApp.ApiService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CasesApp.ApiService.Features.DebtCases.List;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/cases",
                async Task<Ok<CasePage>> (
                    string? status,
                    DateOnly? systemEntryDateFrom,
                    DateOnly? systemEntryDateTo,
                    string? casePrefix,
                    int? page,
                    int? pageSize,
                    DebtCaseDbContext dbContext,
                    CancellationToken cancellationToken) =>
                {
                    var currentPage = page is < 1 or null ? 1 : page.Value;
                    var currentPageSize = pageSize is > 100 or < 1 or null
                        ? 20
                        : pageSize.Value;

                    var query = dbContext.DebtCases.AsNoTracking();

                    if (status is not null)
                    {
                        query = query.Where(item =>
                            item.LifecycleStatus.ToString() == status);
                    }

                    if (systemEntryDateFrom is not null)
                    {
                        query = query.Where(item =>
                            item.CreatedAt >= systemEntryDateFrom.Value.ToDateTime(TimeOnly.MinValue));
                    }

                    if (systemEntryDateTo is not null)
                    {
                        query = query.Where(item =>
                            item.CreatedAt < systemEntryDateTo.Value.AddDays(1)
                                .ToDateTime(TimeOnly.MinValue));
                    }

                    if (casePrefix is not null)
                    {
                        query = query.Where(item =>
                            item.CaseNumber.StartsWith(casePrefix));
                    }

                    var totalItems = await query.CountAsync(cancellationToken);
                    var items = await query
                        .OrderBy(item => item.CaseNumber)
                        .Skip((currentPage - 1) * currentPageSize)
                        .Take(currentPageSize)
                        .Select(item => new DebtCaseResponse(
                            item.CaseNumber,
                            item.LifecycleStatus.ToString(),
                            DateOnly.FromDateTime(item.CreatedAt.DateTime)))
                        .ToListAsync(cancellationToken);

                    return TypedResults.Ok(new CasePage(
                        items,
                        currentPage,
                        currentPageSize,
                        totalItems,
                        (int)Math.Ceiling(totalItems / (double)currentPageSize)));
                })
            .WithName("GetCases")
            .WithSummary("Gets debt cases")
            .Produces<CasePage>(StatusCodes.Status200OK);
    }
}

public sealed record CasePage(
    IReadOnlyList<DebtCaseResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record DebtCaseResponse(
    string CaseNumber,
    string Status,
    DateOnly SystemEntryDate);
