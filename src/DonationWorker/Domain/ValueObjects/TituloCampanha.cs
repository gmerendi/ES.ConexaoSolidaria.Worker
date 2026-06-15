using DonationWorker.Domain.Shared.Exceptions;

namespace DonationWorker.Domain.ValueObjects;

public sealed class TituloCampanha
{
    public const int TamanhoMinimo = 5;
    public const int TamanhoMaximo = 200;

    public string Valor { get; private set; } = null!;

    private TituloCampanha() { }

    private TituloCampanha(string valor) => Valor = valor;

    public static TituloCampanha Create(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DomainException("400_TITLE_REQUIRED");

        var valorTrimado = valor.Trim();

        if (valorTrimado.Length < TamanhoMinimo)
            throw new DomainException("400_TITLE_LENGTH_INVALID");

        if (valorTrimado.Length > TamanhoMaximo)
            throw new DomainException("400_TITLE_LENGTH_INVALID");

        return new TituloCampanha(valorTrimado);
    }

    public static implicit operator string(TituloCampanha titulo) => titulo.Valor;
    public override string ToString() => Valor;
}
