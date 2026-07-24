using DonationWorker.Domain.Enums;
using DonationWorker.Domain.Shared.Entity;
using DonationWorker.Domain.Shared.Helpers;
using DonationWorker.Domain.ValueObjects;
using System.Diagnostics.CodeAnalysis;

namespace DonationWorker.Domain.Entities.Doacoes;

public sealed class Doacao : EntityBase
{
    public Guid GuidUsuario { get; private set; }
    public string NomeUsuario { get; private set; }
    public Email EmailUsuario { get; private set; }
    public Cpf CpfUsuario { get; private set; }
    public Guid GuidCampanha { get; private set; }
    public TituloCampanha TituloCampanha { get; private set; }
    public decimal ValorDoacao { get; private set; }
    public string CorrelationId { get; private set; }
    public DoacaoStatus StatusDoacao { get; private set; }

    private Doacao() { }

    [SetsRequiredMembers]
    public Doacao(Guid guidUsuario, string nomeUsuario, Email emailUsuario, Cpf cpfUsuario,
        Guid guidCampanha, TituloCampanha tituloCampanha, decimal valorDoacao, string correlationId,
        DoacaoStatus statusDoacao)
    {
        NomeAssertions(nomeUsuario);
        EmailAssertions(emailUsuario);
        CpfAssertions(cpfUsuario);
        TituloAssertions(tituloCampanha);
        ValorAssertions(valorDoacao);


        GuidUsuario = guidUsuario;
        NomeUsuario = nomeUsuario;
        EmailUsuario = emailUsuario;
        CpfUsuario = cpfUsuario;
        GuidCampanha = guidCampanha;
        TituloCampanha = tituloCampanha;
        ValorDoacao = valorDoacao;
        CriadoPor = "Worker";
        CorrelationId = correlationId;
        StatusDoacao = statusDoacao;
    }




    // -----------------------------------------------------------------------------
    // Validações
    // -----------------------------------------------------------------------------
    private static void NomeAssertions(string nome)
    {
        AssertionConcern.AssertArgumentNotEmpty(nome, "400_NAME_REQUIRED");
        AssertionConcern.AssertArgumentLength(nome, 5, 200, "400_NAME_LENGTH_INVALID");
    }


    private static void TituloAssertions(TituloCampanha titulo)
    {
        AssertionConcern.AssertArgumentNotEmpty(titulo.Valor, "400_TITLE_REQUIRED");
        AssertionConcern.AssertArgumentLength(titulo.Valor, 0, 200, "400_TITLE_LENGTH_INVALID");
    }

    private static void EmailAssertions(Email email)
    {
        AssertionConcern.AssertArgumentNotNull(email, "400_EMAIL_REQUIRED");
    }


    private static void CpfAssertions(Cpf cpf)
    {
        AssertionConcern.AssertArgumentNotNull(cpf, "400_CPF_REQUIRED");
    }

    private static void ValorAssertions(decimal valor)
    {
        AssertionConcern.AssertArgumentNotLesserOrEqualZero(valor, "400_TITLE_LENGTH_INVALID");
    }

}