using DonationWorker.Domain.Shared.Exceptions;

namespace DonationWorker.Domain.Shared.Helpers
{
    public static class AssertionConcern
    {
        /// <summary>
        /// Checa se o objeto é nulo
        /// </summary>
        /// <param name="objectToCheck">Objeto a ser validado</param>
        /// <param name="errorCode">Código do erro cadastrado no Resource</param>
        /// <exception cref="DomainException">Lançada se o objeto for nulo</exception>
        public static void AssertArgumentNotNull(object objectToCheck, string errorCode)
        {
            if (objectToCheck is null)
            {
                throw new DomainException(errorCode);
            }
        }

        /// <summary>
        /// Checa se a string é nula, vazia ou composta apenas por espaços
        /// </summary>
        /// <param name="stringToCheck">String a ser validada</param>
        /// <param name="errorCode">Código do erro cadastrado no Resource</param>
        /// <exception cref="DomainException">Lançada se a string for vazia</exception>
        public static void AssertArgumentNotEmpty(string stringToCheck, string errorCode)
        {
            if (string.IsNullOrWhiteSpace(stringToCheck))
            {
                throw new DomainException(errorCode);
            }
        }

        /// <summary>
        /// Checa se o tamanho da string está dentro do range permitido (ignora espaços extras nas pontas)
        /// </summary>
        /// <param name="stringToCheck">String a ser validada</param>
        /// <param name="minimum">Tamanho mínimo permitido</param>
        /// <param name="maximum">Tamanho máximo permitido</param>
        /// <param name="errorCode">Código do erro cadastrado no Resource</param>
        /// <exception cref="DomainException">Lançada se o tamanho for inválido</exception>
        public static void AssertArgumentLength(string stringToCheck, int minimum, int maximum, string errorCode)
        {
            int length = stringToCheck?.Trim().Length ?? 0;

            if (length < minimum || length > maximum)
            {
                throw new DomainException(errorCode);
            }
        }

        /// <summary>
        /// Checa se o valor decimal está dentro do range permitido
        /// </summary>
        /// <param name="value">Valor a ser validado</param>
        /// <param name="minimum">Valor mínimo permitido</param>
        /// <param name="maximum">Valor máximo permitido</param>
        /// <param name="errorCode">Código do erro cadastrado no Resource</param>
        /// <exception cref="DomainException">Lançada se o valor estiver fora do range</exception>
        public static void AssertArgumentRange(decimal value, decimal minimum, decimal maximum, string errorCode)
        {
            if (value < minimum || value > maximum)
            {
                throw new DomainException(errorCode);
            }
        }


        /// <summary>
        /// Checa se o valor decimal é maior que 0
        /// </summary>
        /// <param name="value">Valor a ser validado</param>
        /// <param name="minimum">Valor mínimo permitido</param>
        /// <param name="maximum">Valor máximo permitido</param>
        /// <param name="errorCode">Código do erro cadastrado no Resource</param>
        /// <exception cref="DomainException">Lançada se o valor for maior que 0</exception>
        public static void AssertArgumentNotLesserOrEqualZero(decimal value, string errorCode)
        {
            if (value <= 0)
            {
                throw new DomainException(errorCode);
            }
        }
    }
}