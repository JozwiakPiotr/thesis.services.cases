using CasesApp.ApiService.Domain;
using CasesApp.ApiService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CasesApp.ApiService.Features.DebtCases.Create;

public static class Endpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/api/cases",
                async Task<NoContent> (
                    Request request,
                    DebtCaseDbContext dbContext,
                    CancellationToken cancellationToken) =>
                {
                    var debtCase = DebtCase.Create(
                        Guid.NewGuid(),
                        request.CreditorId,
                        request.ExternalReferenceNumber,
                        request.DocumentNumber,
                        request.IssueDate,
                        request.DueDate,
                        new Debtor(
                            request.Debtor.Name,
                            request.Debtor.TaxId,
                            request.Debtor.Addresses
                                .Select(address => new Address(
                                    address.Country,
                                    address.City,
                                    address.PostalCode,
                                    address.Street,
                                    address.BuildingNumber,
                                    address.ApartmentNumber))
                                .ToList()));

                    await dbContext.DebtCases.AddAsync(debtCase, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    return TypedResults.NoContent();
                })
            .WithName("AddCase")
            .WithSummary("Adds new debt case")
            .Produces(StatusCodes.Status204NoContent);
    }
}

public sealed record Request
{
    public Guid CreditorId { get; init; }
    public required DebtorRequest Debtor { get; init; }
    public required string ExternalReferenceNumber { get; init; }
    public required string DocumentNumber { get; init; }
    public DateOnly IssueDate { get; init; }
    public DateOnly DueDate { get; init; }
}

public sealed record DebtorRequest
{
    public string Type { get; init; } = "INDIVIDUAL";
    public required string Name { get; init; }
    public required string TaxId { get; init; }
    public required IReadOnlyList<AddressRequest> Addresses { get; init; }
}

public sealed record AddressRequest
{
    public required string Country { get; init; }
    public required string City { get; init; }
    public required string PostalCode { get; init; }
    public string? Street { get; init; }
    public required string BuildingNumber { get; init; }
    public string? ApartmentNumber { get; init; }
}
