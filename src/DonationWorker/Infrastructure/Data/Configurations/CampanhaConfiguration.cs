using DonationWorker.Domain.Entities.Campanhas;
using DonationWorker.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonationWorker.Infrastructure.Data.Configurations;

public sealed class CampanhaConfiguration : IEntityTypeConfiguration<Campanha>
{
    public void Configure(EntityTypeBuilder<Campanha> builder)
    {
        builder.ToTable("campanha", schema: "operacao");

        // Chave Primária
        builder.HasKey(u => u.Guid);
        builder.Property(u => u.Guid)
            .HasColumnType("uuid")
            .HasColumnName("guid");

        // Propriedades Tradicionais
        builder.Property(c => c.Descricao)
            .HasMaxLength(2000)
            .HasColumnType("varchar(2000)")
            .HasColumnName("descricao")
            .IsRequired();

        builder.Property(u => u.DataInicio)
                .HasColumnName("data_inicio")
                .IsRequired()
                .HasColumnType("timestamp with time zone");

        builder.Property(u => u.DataFim)
                .HasColumnName("data_fim")
                .IsRequired()
                .HasColumnType("timestamp with time zone");

        builder.Property(c => c.ValorArrecadado)
                .HasColumnName("valor_arrecadado")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

        // Mapeamento de Enums como String
        builder.Property(u => u.Status)
               .HasColumnName("status")
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(30)
               .HasColumnType("varchar(30)");

        builder.Property(u => u.StatusCampanha)
               .HasColumnName("status_campanha")
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(30)
               .HasColumnType("varchar(30)");

        // Propriedades de Auditoria
        builder.Property(u => u.CriadoPor)
                .HasColumnName("criado_por")
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnType("varchar(150)");

        builder.Property(u => u.DataCriacao)
                .HasColumnName("data_criacao")
                .IsRequired()
                .HasColumnType("timestamp with time zone");

        builder.Property(u => u.ModificadoPor)
                .HasColumnName("modificado_por")
                .HasMaxLength(150)
                .HasColumnType("varchar(150)");

        builder.Property(u => u.DataModificacao)
                .HasColumnName("data_modificacao")
                .HasColumnType("timestamp with time zone");

        // Mapeamento dos Value Objects 
        builder.OwnsOne(c => c.Titulo, titulo =>
        {
            titulo.Property(t => t.Valor)
                .HasColumnName("titulo")
                .HasMaxLength(TituloCampanha.TamanhoMaximo)
                .IsRequired()
                .HasColumnType("varchar(200)");
        });

        builder.OwnsOne(c => c.MetaFinanceira, meta =>
        {
            meta.Property(m => m.Valor)
                .HasColumnName("meta_financeira")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
        });

    }
}
