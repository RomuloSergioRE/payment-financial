namespace Payment.Application.Features.Plans.Queries.GetPlanPrices;

// Response DTO representing a plan with its name, price, and currency.
public sealed record GetPlanPricesResponse(
    string PlanType,
    string Name,
    decimal Price,
    string Currency);
