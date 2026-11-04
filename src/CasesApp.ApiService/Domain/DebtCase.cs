namespace CasesApp.ApiService.Domain;

public sealed class DebtCase
{
    private DebtCase()
    {
    }

    private DebtCase(
        Guid id,
        string caseNumber,
        Guid creditorId,
        string externalReferenceNumber,
        string documentNumber,
        DateOnly issueDate,
        DateOnly dueDate,
        Debtor debtor)
    {
        Id = id;
        CaseNumber = caseNumber;
        CreditorId = creditorId;
        ExternalReferenceNumber = externalReferenceNumber;
        DocumentNumber = documentNumber;
        IssueDate = issueDate;
        DueDate = dueDate;
        Debtor = debtor;
        LifecycleStatus = DebtCaseLifecycleStatus.Draft;
        OperationalStatus = DebtCaseOperationalStatus.New;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string CaseNumber { get; private set; } = null!;
    public Guid CreditorId { get; private set; }
    public string ExternalReferenceNumber { get; private set; } = null!;
    public string DocumentNumber { get; private set; } = null!;
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public DebtCaseLifecycleStatus LifecycleStatus { get; private set; }
    public DebtCaseOperationalStatus OperationalStatus { get; private set; }
    public DateTimeOffset? ActivatedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Debtor Debtor { get; private set; } = null!;

    public static DebtCase Create(
        Guid id,
        Guid creditorId,
        string externalReferenceNumber,
        string documentNumber,
        DateOnly issueDate,
        DateOnly dueDate,
        Debtor debtor) =>
        new(
            id,
            $"CASE-{id:N}",
            creditorId,
            externalReferenceNumber,
            documentNumber,
            issueDate,
            dueDate,
            debtor);

    public void Activate(DateTimeOffset activatedAt)
    {
        if (LifecycleStatus != DebtCaseLifecycleStatus.Draft)
        {
            throw new InvalidOperationException(
                $"A case in status '{LifecycleStatus}' cannot be activated.");
        }

        LifecycleStatus = DebtCaseLifecycleStatus.Active;
        ActivatedAt = activatedAt;
    }
}
