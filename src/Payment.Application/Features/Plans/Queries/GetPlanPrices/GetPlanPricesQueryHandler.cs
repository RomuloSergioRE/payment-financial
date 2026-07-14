using MediatR;

namespace Payment.Application.Features.Plans.Queries.GetPlanPrices;

// Handles GetPlanPricesQuery by returning the hardcoded plan pricing list.
public sealed class GetPlanPricesQueryHandler
    : IRequestHandler<GetPlanPricesQuery, List<GetPlanPricesResponse>>
{
    // Returns the list of available plans with their prices.
    public Task<List<GetPlanPricesResponse>> Handle(
        GetPlanPricesQuery request,
        CancellationToken cancellationToken)
    {
        var plans = new List<GetPlanPricesResponse>
        {
            new("pro", "Pro", 29.90m, "BRL"),
            new("enterprise", "Enterprise", 99.00m, "BRL")
        };

        return Task.FromResult(plans);
    }
}
