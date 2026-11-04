namespace PaymentsApp.ApiService.Domain;

public sealed class FinancialAccount
{
    private FinancialAccount()
    {
    }

    private FinancialAccount(
        Guid accountId,
        Guid caseId,
        string currency,
        decimal principalOriginal,
        decimal interestAccrued,
        decimal additionalCosts,
        DateOnly handoverDate,
        DateTimeOffset createdAt)
    {
        AccountId = accountId;
        CaseId = caseId;
        Currency = currency;
        PrincipalOriginal = principalOriginal;
        InterestAccrued = interestAccrued;
        AdditionalCosts = additionalCosts;
        HandoverDate = handoverDate;
        CreatedAt = createdAt;
    }

    public Guid AccountId { get; private set; }
    public Guid CaseId { get; private set; }
    public string Currency { get; private set; } = null!;
    public decimal PrincipalOriginal { get; private set; }
    public decimal PrincipalPaid { get; private set; }
    public decimal InterestAccrued { get; private set; }
    public decimal InterestPaid { get; private set; }
    public decimal AdditionalCosts { get; private set; }
    public decimal AdditionalCostsPaid { get; private set; }
    public DateOnly HandoverDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static FinancialAccount Create(
        Guid caseId,
        string currency,
        decimal principalOriginal,
        decimal interestAccrued,
        decimal additionalCosts,
        DateOnly handoverDate,
        DateTimeOffset createdAt) =>
        new(
            Guid.NewGuid(),
            caseId,
            currency,
            principalOriginal,
            interestAccrued,
            additionalCosts,
            handoverDate,
            createdAt);
}
