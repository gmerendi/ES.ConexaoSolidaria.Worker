using DonationWorker.Domain.Shared.Resources;

namespace DonationWorker.Domain.Shared.Exceptions
{
    public class DomainException : Exception
    {
        public string ErrorCode { get; }

        // 1. Sobrecarga com mensagem padrão buscada do Resource 
        public DomainException(string errorCode)
            : this(errorCode, ErrorMessages.GetString(errorCode))
        {
        }

        // 2. Mensagem customizada legível informada manualmente + código de erro
        public DomainException(string errorCode, string message) : base(message)
        {
            // Garante que o ErrorCode nunca fique em branco e padroniza em caixa alta
            ErrorCode = string.IsNullOrWhiteSpace(errorCode)
                ? "DOMAIN_ERROR"
                : errorCode.ToUpperInvariant();
        }

        // 3. Permitir passar uma InnerException + mensagem buscada do Resource
        public DomainException(string errorCode, Exception innerException)
            : this(errorCode, ErrorMessages.GetString(errorCode), innerException)
        {
        }

        // 4. Permitir passar uma InnerException com mensagem customizada manual
        public DomainException(string errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = string.IsNullOrWhiteSpace(errorCode)
                ? "DOMAIN_ERROR"
                : errorCode.ToUpperInvariant();
        }
    }
}