using System.Resources;

namespace DonationWorker.Domain.Shared.Resources
{
    public static class ErrorMessages
    {
        // Vincula ao ResourceManager gerado automaticamente pelo arquivo Errors.resx
        private static readonly ResourceManager _resourceManager =
            new ResourceManager(typeof(Errors));

        public static string GetString(string errorCode)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
                return "Ocorreu um erro inesperado.";

            // Busca o texto no arquivo .resx baseado na chave string
            string? message = _resourceManager.GetString(errorCode.ToUpperInvariant());

            // Se o programador passar um código que não existe no arquivo, retorna um fallback amigável
            return message ?? $"Erro não catalogado: {errorCode}";
        }
    }
}