using DonationWorker.Domain.Entities.Doacoes;
using DonationWorker.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonationWorker.Infrastructure.Data.Configurations;

public sealed class DoacaoConfiguration : IEntityTypeConfiguration<Doacao>
{
    public void Configure(EntityTypeBuilder<Doacao> builder)
    {
        builder.ToTable("doacao", schema: "operacao");

        // Chave Primária
        builder.HasKey(u => u.Guid);
        builder.Property(u => u.Guid)
            .HasColumnType("uuid")
            .HasColumnName("guid");

        // Propriedades Tradicionais
        builder.Property(u => u.GuidUsuario)
            .HasColumnType("uuid")
            .HasColumnName("guid_usuario")
            .IsRequired();

        builder.Property(u => u.NomeUsuario)
               .HasColumnName("nome_usuario")
               .IsRequired()
               .HasMaxLength(200)
               .HasColumnType("varchar(200)");

        builder.Property(u => u.GuidCampanha)
            .HasColumnType("uuid")
            .HasColumnName("guid_campanha")
            .IsRequired();

        builder.Property(c => c.ValorDoacao)
                .HasColumnName("valor_doacao")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

        builder.Property(u => u.CorrelationId)
               .HasColumnName("correlation_id")
               .IsRequired()
               .HasMaxLength(200)
               .HasColumnType("varchar(100)");

        // Mapeamento de Enums como String
        builder.Property(u => u.Status)
               .HasColumnName("status")
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(30)
               .HasColumnType("varchar(30)");

        builder.Property(u => u.StatusDoacao)
               .HasColumnName("status_doacao")
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
        builder.OwnsOne(c => c.TituloCampanha, titulo =>
        {
            titulo.Property(t => t.Valor)
                .HasColumnName("titulo_campanha")
                .HasMaxLength(TituloCampanha.TamanhoMaximo)
                .IsRequired()
                .HasColumnType("varchar(200)");
        });

        builder.OwnsOne(u => u.EmailUsuario, emailBuilder =>
        {
            emailBuilder.Property(e => e.Endereco)
                .HasColumnName("email_usuario")
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnType("varchar(150)");
        });


        builder.OwnsOne(u => u.CpfUsuario, cpfBuilder =>
        {
            cpfBuilder.Property(c => c.Numero)
                .HasColumnName("cpf_usuario")
                .IsRequired()
                .HasMaxLength(11)
                .HasColumnType("varchar(11)");
        });
    }
}
