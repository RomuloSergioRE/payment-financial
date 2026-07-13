using MediatR;
using Payment.Application.Common.Behaviours;

namespace Payment.Application.Features.Plans.Queries.GetPlanPrices;

public sealed record GetPlanPricesQuery()
    : IRequest<List<GetPlanPricesResponse>>, ICachableRequest
{
    public string CacheKey => "plan:prices";
    public TimeSpan? CacheExpiration => TimeSpan.FromMinutes(30);
}
