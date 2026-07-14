using MediatR;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Common.Interfaces;

namespace Payment.Application.Common.Behaviours;

// Wraps the request handler in a database transaction that commits on success or rolls back on failure.
// Only activates when TRequest implements ITransactionalRequest — read-only queries bypass this behaviour.
public sealed class TransactionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, ITransactionalRequest
{
    private readonly IPaymentDbContext _context;

    public TransactionBehaviour(IPaymentDbContext context)
        => _context = context;

    // Executes the handler inside a transaction with EF Core's retry strategy for transient failures.
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // ExecutionStrategy retries automatically on transient connection failures.
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var response = await next();

                // Persist all changes accumulated by the handler, then commit.
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return response;
            }
            catch
            {
                // Undo any changes on exception.
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
