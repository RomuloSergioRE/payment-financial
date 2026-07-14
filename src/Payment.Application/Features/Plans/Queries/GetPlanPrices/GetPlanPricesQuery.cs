using MediatR;
using Payment.Application.Common.Interfaces;

namespace Payment.Application.Features.Plans.Queries.GetPlanPrices;

// Query to retrieve available plan prices. Results are cached for 30 minutes.
public sealed record GetPlanPricesQuery()
    : IRequest<List<GetPlanPricesResponse>>, ICachableRequest
{
    public string CacheKey => "plan:prices";
    public TimeSpan? CacheExpiration => TimeSpan.FromMinutes(30);
}
