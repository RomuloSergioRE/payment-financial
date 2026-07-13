using MediatR;

namespace Payment.Application.Features.Plans.Queries.GetPlanPrices;

public sealed class GetPlanPricesQueryHandler
    : IRequestHandler<GetPlanPricesQuery, List<GetPlanPricesResponse>>
{
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
