namespace CasesApp.ApiService.Domain;

public sealed class Debtor
{
    private Debtor()
    {
    }

    public Debtor(
        string name,
        string taxId,
        ICollection<Address> addresses)
    {
        Type = "INDIVIDUAL";
        Name = name;
        TaxId = taxId;
        Addresses = addresses;
    }

    public int Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string TaxId { get; private set; } = null!;
    public ICollection<Address> Addresses { get; private set; } = [];
}
