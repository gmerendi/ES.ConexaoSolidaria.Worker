using DonationWorker.Domain.Entities.Campanhas.Enums;
using DonationWorker.Domain.Shared.Entity;
using DonationWorker.Domain.Shared.Exceptions;
using DonationWorker.Domain.Shared.Helpers;
using DonationWorker.Domain.ValueObjects;
using System.Diagnostics.CodeAnalysis;

namespace DonationWorker.Domain.Entities.Campanhas;

public sealed class Campanha : EntityBase
{
    public TituloCampanha Titulo { get; private set; } = null!;
    public string Descricao { get; private set; } = String.Empty;
    public MetaFinanceira MetaFinanceira { get; private set; } = null!;
    public decimal ValorArrecadado { get; private set; }
    public DateTime DataInicio { get; private set; }
    public DateTime DataFim { get; private set; }
    public CampanhaStatus StatusCampanha { get; private set; }

    private Campanha() { } // Para EF Core

    [SetsRequiredMembers]
    public Campanha(TituloCampanha titulo, string descricao, MetaFinanceira metaFinanceira,
        DateTime dataInicio, DateTime dataFim, string criadoPor)
    {
        // Validacoes
        TituloAssertions(titulo);
        DescricaoAssertions(descricao);
        MetaFinanceiraAssertions(metaFinanceira);
        DataAssertions(dataInicio, dataFim);
        SolicitanteAssertions(criadoPor);

        Titulo = titulo;
        Descricao = descricao;
        MetaFinanceira = metaFinanceira;
        ValorArrecadado = 0;
        DataInicio = dataInicio;
        DataFim = dataFim;
        StatusCampanha = CampanhaStatus.ATIVA;
        CriadoPor = criadoPor;
    }




    // -----------------------------------------------------------------------------
    // Metodos
    // -----------------------------------------------------------------------------
    public void AlterarCampanha(TituloCampanha titulo, string descricao, MetaFinanceira metaFinanceira,
        DateTime dataInicio, DateTime dataFim, string modificadoPor)
    {
        TituloAssertions(titulo);
        DescricaoAssertions(descricao);
        MetaFinanceiraAssertions(metaFinanceira);
        DataAssertions(dataInicio, dataFim);
        SolicitanteAssertions(modificadoPor);


        if (StatusCampanha != CampanhaStatus.ATIVA)
        {
            throw new DomainException("422_CAMPAIGN_ACTIVE_CAN_BE_EDITED");
        }

        Titulo = titulo;
        Descricao = descricao.Trim();
        MetaFinanceira = metaFinanceira;
        DataInicio = dataInicio;
        DataFim = dataFim;
        ModificadoPor = modificadoPor;
    }


    public void CancelarCampanha(string modificadoPor)
    {
        SolicitanteAssertions(modificadoPor);

        if (StatusCampanha == CampanhaStatus.CANCELADA)
        {
            throw new DomainException("422_CAMPAIGN_ALREADY_CANCELLED");
        }
            

        if (StatusCampanha == CampanhaStatus.CONCLUIDA)
        {
            throw new DomainException("422_CAMPAIGN_FINISHED_CANNOT_CANCEL");
        }       
  
        StatusCampanha = CampanhaStatus.CANCELADA;
        ModificadoPor = modificadoPor;
    }

    public void ConcluirCampanha(string modificadoPor)
    {
        if (StatusCampanha != CampanhaStatus.ATIVA)
        {
            throw new DomainException("422_CAMPAIGN_ACTIVE_CAN_BE_FINISHED");
        }
            
        StatusCampanha = CampanhaStatus.CONCLUIDA;
        ModificadoPor = modificadoPor;
    }



    // -----------------------------------------------------------------------------
    // Validações
    // -----------------------------------------------------------------------------
    private static void TituloAssertions(TituloCampanha titulo)
    {
        AssertionConcern.AssertArgumentNotEmpty(titulo.Valor, "400_TITLE_REQUIRED");
        AssertionConcern.AssertArgumentLength(titulo.Valor, 0, 200, "400_TITLE_LENGTH_INVALID");
    }


    private static void DescricaoAssertions(string descricao)
    {
        AssertionConcern.AssertArgumentNotEmpty(descricao, "400_DESCRIPTION_REQUIRED");
        AssertionConcern.AssertArgumentLength(descricao, 0, 2000, "400_DESCRIPTION_LENGTH_INVALID");
    }

    private static void MetaFinanceiraAssertions(MetaFinanceira meta)
    {
        AssertionConcern.AssertArgumentNotNull(meta, "400_TARGET_REQUIRED");
    }

    private static void DataAssertions(DateTime inicio, DateTime fim)
    {
        AssertionConcern.AssertArgumentNotNull(inicio, "400_STARTDATE_REQUIRED");
        AssertionConcern.AssertArgumentNotNull(inicio, "400_ENDDATE_REQUIRED");

        if (fim <= inicio)
        {
            throw new DomainException("422_DATES_MISMATCHING");
        }
    }

    private static void SolicitanteAssertions(string solicitanteEmail)
    {
        AssertionConcern.AssertArgumentNotEmpty(solicitanteEmail, "400_REQUESTER_REQUIRED");//Aqui já nao testa se o solicitante existe
    }
}
