using DonationWorker.Domain.Enums;

namespace DonationWorker.Domain.Shared.Entity
{
    public class EntityBase
    {
        public Guid Guid { get; set; }
        public DateTime DataCriacao { get; set; }
        public required string CriadoPor { get; set; }
        public string? ModificadoPor { get; set; }
        public DateTime? DataModificacao { get; set; }
        public EntityStatus Status { get; set; } = EntityStatus.ACTIVE;
    }
}
