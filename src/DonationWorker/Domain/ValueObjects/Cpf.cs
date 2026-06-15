using DonationWorker.Domain.Shared.Exceptions;
using DonationWorker.Domain.Shared.Helpers;
using DonationWorker.Domain.Shared.Primitives;
using DonationWorker.Domain.Shared.ValueObjects;

namespace DonationWorker.Domain.ValueObjects
{
    public class Cpf : ValueObject
    {
        // Mantemos o estado interno como string de 11 dígitos purificados
        public string Numero { get; private set; }

        protected Cpf() { } // Para EF Core

        public Cpf(string numero)
        {
            Numero = numero;
        }




        // -----------------------------------------------------------------------------
        // Metodos
        // -----------------------------------------------------------------------------
        public static Cpf Create(string cpfRaw)
        {
            AssertionConcern.AssertArgumentNotEmpty(cpfRaw, "400_CPF_REQUIRED");


            // Remove pontos, traços ou espaços que o usuário possa ter digitado no front
            // Exemplo: "012.345.678-90" vira "01234567890" (o zero à esquerda está seguro aqui!)
            string cpfLimpo = new string(cpfRaw.Where(char.IsDigit).ToArray());

            // Um CPF PRECISA ter exatamente 11 caracteres numéricos
            AssertionConcern.AssertArgumentLength(cpfLimpo, 11, 11, "422_CPF_INVALID_LENGTH");

            // Faz a validação matemática dos dígitos verificadores
            if (!ValidateCpfStructure(cpfLimpo))
            {
                throw new DomainException("422_CPF_INVALID");
            }

            return new Cpf(cpfLimpo);
        }

        public static string Anonymize(string cpfRaw)
        {
            if (string.IsNullOrWhiteSpace(cpfRaw))
                return string.Empty;

            string cleanCpf = new string(cpfRaw.Where(char.IsDigit).ToArray());

            if (cleanCpf.Length != 11)
                return "";

            string tresPrimeirosDigitos = cleanCpf.Substring(0, 3);

            return $"{tresPrimeirosDigitos}********";
        }


        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Numero;
        }


        // -----------------------------------------------------------------------------
        // Validações
        // -----------------------------------------------------------------------------
        private static bool ValidateCpfStructure(string cpf)
        {
            // Ignora sequências idênticas conhecidas
            if (new string(cpf[0], 11) == cpf)
            {
                return false;
            }

            // ---- Primeiro Dígito Verificador ----
            int[] multiplicadores1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma = 0;

            for (int i = 0; i < 9; i++)
            {
                soma += (cpf[i] - '0') * multiplicadores1[i];
            }

            int resto = soma % 11;
            int primeiroDigito = resto < 2 ? 0 : 11 - resto;

            if (cpf[9] - '0' != primeiroDigito)
            {
                return false;
            }

            // ---- Segundo Dígito Verificador ----
            int[] multiplicadores2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            soma = 0;

            for (int i = 0; i < 10; i++)
            {
                soma += (cpf[i] - '0') * multiplicadores2[i];
            }

            resto = soma % 11;
            int segundoDigito = resto < 2 ? 0 : 11 - resto;

            return cpf[10] - '0' == segundoDigito;
        }
    }
}