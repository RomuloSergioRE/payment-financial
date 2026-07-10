using MediatR;

namespace Payment.Application.Features.Plans.Queries.GetPlanPrices;

public sealed record GetPlanPricesQuery()
    : IRequest<List<GetPlanPricesResponse>>;
