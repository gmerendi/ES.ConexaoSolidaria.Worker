using DonationWorker.Domain.Entities.Campanhas;
using DonationWorker.Domain.ValueObjects;

namespace DonationWorker.Tests.Common;

/// <summary>
/// Helpers e dados de apoio reutilizados pelos testes (CPFs válidos calculados
/// segundo o mesmo algoritmo de módulo 11 implementado em <see cref="Cpf"/>,
/// builders de Value Objects e factories de entidades para reduzir duplicação).
/// </summary>
public static class TestData
{
    // CPFs validos (digitos verificadores calculados via modulo 11)
    public const string CpfValido1 = "11144477735";
    public const string CpfValido2 = "52998224725";
    public const string CpfValidoFormatado = "111.444.777-35"; // mesmo numero, com mascara

    // CPFs invalidos
    public const string CpfDigitosInvalidos = "12345678900"; // estrutura/digito verificador invalido
    public const string CpfSequenciaRepetida = "11111111111"; // sequencia repetida, invalida por regra explicita
    public const string CpfTamanhoInvalido = "123456789"; // menos de 11 digitos

    public const string EmailValido = "doador@exemplo.com";
    public const string EmailInvalido = "doador-sem-arroba.com";

    public static Email CriarEmailValido(string endereco = EmailValido) => Email.Create(endereco);

    public static Cpf CriarCpfValido(string numero = CpfValido1) => Cpf.Create(numero);

    public static TituloCampanha CriarTituloValido(string valor = "Campanha de inverno solidario")
        => TituloCampanha.Create(valor);

    public static MetaFinanceira CriarMetaValida(decimal valor = 10_000m)
        => MetaFinanceira.Create(valor);

    /// <summary>
    /// Cria uma instancia valida de <see cref="Campanha"/> usando os builders acima,
    /// com datas padrao (inicio hoje, fim em 30 dias) e status ATIVA.
    /// </summary>
    public static Campanha CriarCampanhaValida(
        string titulo = "Campanha de inverno solidario",
        string descricao = "Arrecadacao de roupas e cobertores para o inverno.",
        decimal meta = 10_000m,
        DateTime? dataInicio = null,
        DateTime? dataFim = null,
        string criadoPor = "gestor@ong.com")
    {
        var inicio = dataInicio ?? DateTime.UtcNow;
        var fim = dataFim ?? inicio.AddDays(30);

        return new Campanha(
            CriarTituloValido(titulo),
            descricao,
            CriarMetaValida(meta),
            inicio,
            fim,
            criadoPor);
    }
}
