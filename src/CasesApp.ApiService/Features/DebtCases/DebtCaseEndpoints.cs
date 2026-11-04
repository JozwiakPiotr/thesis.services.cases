namespace CasesApp.ApiService.Features.DebtCases;

public static class DebtCaseEndpoints
{
    public static IEndpointRouteBuilder MapDebtCaseEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        Create.Endpoint.Map(endpoints);
        List.Endpoint.Map(endpoints);
        Activate.Endpoint.Map(endpoints);

        return endpoints;
    }
}
