namespace Payment.Application.Common.Interfaces;

// Marca queries cuja resposta deve ser cacheada em memória.
// O CachingBehavior verifica o cache antes de executar o handler
// e armazena o resultado após a execução.
public interface ICachableRequest
{
    string CacheKey { get; }
    TimeSpan? CacheExpiration { get; }
}
