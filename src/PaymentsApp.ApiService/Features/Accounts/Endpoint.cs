using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PaymentsApp.ApiService.Domain;
using PaymentsApp.ApiService.Infrastructure.Persistence;

namespace PaymentsApp.ApiService.Features.Accounts;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/api/accounts",
                async Task<Results<Created<AccountResponse>, BadRequest, Conflict>> (
                    CreateAccountRequest request,
                    PaymentsDbContext dbContext,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    if (await dbContext.Accounts.AnyAsync(
                            account => account.CaseId == request.CaseId,
                            cancellationToken))
                    {
                        return TypedResults.Conflict();
                    }

                    var account = FinancialAccount.Create(
                        request.CaseId,
                        request.Currency,
                        request.PrincipalOriginal,
                        request.InterestAccrued,
                        request.AdditionalCosts,
                        request.HandoverDate,
                        timeProvider.GetUtcNow());

                    await dbContext.Accounts.AddAsync(account, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    var response = new AccountResponse(
                        account.AccountId,
                        account.CaseId,
                        account.Currency,
                        account.CreatedAt);

                    return TypedResults.Created(
                        $"/api/accounts/{account.CaseId}/balance",
                        response);
                })
            .WithName("CreateAccount")
            .WithSummary("Creates a financial account")
            .Produces<AccountResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        endpoints.MapGet(
                "/api/accounts/{caseId:guid}/balance",
                async Task<Results<Ok<AccountBalanceResponse>, NotFound>> (
                    Guid caseId,
                    PaymentsDbContext dbContext,
                    CancellationToken cancellationToken) =>
                {
                    var account = await dbContext.Accounts
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            item => item.CaseId == caseId,
                            cancellationToken);

                    if (account is null)
                    {
                        return TypedResults.NotFound();
                    }

                    var principalRemaining =
                        account.PrincipalOriginal - account.PrincipalPaid;
                    var interestRemaining =
                        account.InterestAccrued - account.InterestPaid;
                    var costsRemaining =
                        account.AdditionalCosts - account.AdditionalCostsPaid;

                    return TypedResults.Ok(new AccountBalanceResponse(
                        account.CaseId,
                        account.Currency,
                        new PrincipalBalance(
                            account.PrincipalOriginal,
                            account.PrincipalPaid,
                            principalRemaining),
                        new InterestBalance(
                            account.InterestAccrued,
                            account.InterestPaid,
                            interestRemaining),
                        costsRemaining,
                        principalRemaining + interestRemaining + costsRemaining));
                })
            .WithName("GetAccountBalance")
            .WithSummary("Gets account balance")
            .Produces<AccountBalanceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}

/// <summary>Initial financial positions assigned to a debt case.</summary>
public sealed record CreateAccountRequest
{
    [Required]
    public Guid CaseId { get; init; }

    [Required, StringLength(3, MinimumLength = 3)]
    public required string Currency { get; init; }

    [Range(0, double.MaxValue)]
    public decimal PrincipalOriginal { get; init; }

    [Range(0, double.MaxValue)]
    public decimal InterestAccrued { get; init; }

    [Range(0, double.MaxValue)]
    public decimal AdditionalCosts { get; init; }

    [Required]
    public DateOnly HandoverDate { get; init; }
}

/// <summary>Financial account created for a debt case.</summary>
public sealed record AccountResponse(
    Guid AccountId,
    Guid CaseId,
    string Currency,
    DateTimeOffset CreatedAt);

/// <summary>Current financial balance of a debt case.</summary>
public sealed record AccountBalanceResponse(
    Guid CaseId,
    string Currency,
    PrincipalBalance Principal,
    InterestBalance Interest,
    decimal CostsRemaining,
    decimal TotalRemaining);

/// <summary>Principal balance split into original, paid, and remaining amounts.</summary>
public sealed record PrincipalBalance(
    decimal Original,
    decimal Paid,
    decimal Remaining);

/// <summary>Interest balance split into accrued, paid, and remaining amounts.</summary>
public sealed record InterestBalance(
    decimal Accrued,
    decimal Paid,
    decimal Remaining);
