namespace Payment.Application.Common.Interfaces;

// Marca commands que devem gerar uma mensagem no outbox.
// O OutboxBehavior serializa a response e salva como OutboxMessage no banco,
// para publicação assíncrona pelo OutboxProcessor.
public interface IPublishableRequest { }
