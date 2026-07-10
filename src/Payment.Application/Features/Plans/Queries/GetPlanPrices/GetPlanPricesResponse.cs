namespace Payment.Application.Features.Plans.Queries.GetPlanPrices;

public sealed record GetPlanPricesResponse(
    string PlanType,
    string Name,
    decimal Price,
    string Currency);
