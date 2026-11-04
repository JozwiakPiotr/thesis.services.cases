namespace CasesApp.ApiService.Domain;

public sealed class Address
{
    private Address()
    {
    }

    public Address(
        string country,
        string city,
        string postalCode,
        string? street,
        string buildingNumber,
        string? apartmentNumber)
    {
        Country = country;
        City = city;
        PostalCode = postalCode;
        Street = street;
        BuildingNumber = buildingNumber;
        ApartmentNumber = apartmentNumber;
    }

    public int Id { get; private set; }
    public string Country { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string PostalCode { get; private set; } = null!;
    public string? Street { get; private set; }
    public string BuildingNumber { get; private set; } = null!;
    public string? ApartmentNumber { get; private set; }
}
