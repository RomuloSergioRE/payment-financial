namespace Payment.Application.Common.Interfaces;

// Marca commands que devem ser executados dentro de uma transação no banco.
// Queries (read-only) NÃO implementam esta interface — rodam sem transação.
public interface ITransactionalRequest { }
