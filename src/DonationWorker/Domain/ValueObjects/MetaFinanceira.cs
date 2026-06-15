using DonationWorker.Domain.Shared.Exceptions;

namespace DonationWorker.Domain.Entities.Campanhas;

public sealed class MetaFinanceira
{
    public decimal Valor { get; private set; }

    private MetaFinanceira() { }

    private MetaFinanceira(decimal valor) => Valor = valor;

    public static MetaFinanceira Create(decimal valor)
    {
        if (valor <= 0)
            throw new DomainException("400_TARGET_RANGE_INVALID");

        return new MetaFinanceira(valor);
    }

    public static implicit operator decimal(MetaFinanceira meta) => meta.Valor;
    public override string ToString() => Valor.ToString("C2");
}
