using DonationWorker.Domain.Entities.Campanhas;
using DonationWorker.Domain.Entities.Campanhas.Enums;
using DonationWorker.Domain.Shared.Exceptions;
using DonationWorker.Tests.Common;
using FluentAssertions;
using Xunit;

namespace DonationWorker.Tests.Domain.Entities;

public class CampanhaTests
{
    // -----------------------------------------------------------------------------
    // Construtor
    // -----------------------------------------------------------------------------
    [Fact]
    public void Construtor_ComDadosValidos_DeveCriarCampanhaAtivaComValorArrecadadoZero()
    {
        // Arrange
        var titulo = TestData.CriarTituloValido("Campanha de inverno solidario");
        var meta = TestData.CriarMetaValida(5_000m);
        var inicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var campanha = new Campanha(titulo, "Descricao da campanha", meta, inicio, fim, "gestor@ong.com");

        // Assert
        campanha.Titulo.Should().Be(titulo);
        campanha.Descricao.Should().Be("Descricao da campanha");
        campanha.MetaFinanceira.Should().Be(meta);
        campanha.ValorArrecadado.Should().Be(0);
        campanha.DataInicio.Should().Be(inicio);
        campanha.DataFim.Should().Be(fim);
        campanha.StatusCampanha.Should().Be(CampanhaStatus.ATIVA);
        campanha.CriadoPor.Should().Be("gestor@ong.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Construtor_ComDescricaoNulaOuVazia_DeveLancarDomainException(string? descricaoInvalida)
    {
        // Arrange
        var titulo = TestData.CriarTituloValido();
        var meta = TestData.CriarMetaValida();

        // Act
        var act = () => new Campanha(titulo, descricaoInvalida!, meta, DateTime.UtcNow, DateTime.UtcNow.AddDays(10), "gestor@ong.com");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("400_DESCRIPTION_REQUIRED");
    }

    [Fact]
    public void Construtor_ComDescricaoMaiorQue2000Caracteres_DeveLancarDomainException()
    {
        // Arrange
        var titulo = TestData.CriarTituloValido();
        var meta = TestData.CriarMetaValida();
        var descricaoLonga = new string('a', 2001);

        // Act
        var act = () => new Campanha(titulo, descricaoLonga, meta, DateTime.UtcNow, DateTime.UtcNow.AddDays(10), "gestor@ong.com");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("400_DESCRIPTION_LENGTH_INVALID");
    }

    [Fact]
    public void Construtor_ComMetaFinanceiraNula_DeveLancarDomainException()
    {
        // Arrange
        var titulo = TestData.CriarTituloValido();

        // Act
        var act = () => new Campanha(titulo, "Descricao valida", null!, DateTime.UtcNow, DateTime.UtcNow.AddDays(10), "gestor@ong.com");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("400_TARGET_REQUIRED");
    }

    [Fact]
    public void Construtor_ComDataFimAnteriorOuIgualADataInicio_DeveLancarDomainException()
    {
        // Arrange
        var titulo = TestData.CriarTituloValido();
        var meta = TestData.CriarMetaValida();
        var inicio = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        var fimIgualAoInicio = inicio;

        // Act
        var act = () => new Campanha(titulo, "Descricao valida", meta, inicio, fimIgualAoInicio, "gestor@ong.com");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("422_DATES_MISMATCHING");
    }

    [Fact]
    public void Construtor_ComDataFimAnteriorADataInicio_DeveLancarDomainException()
    {
        // Arrange
        var titulo = TestData.CriarTituloValido();
        var meta = TestData.CriarMetaValida();
        var inicio = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        var fimAnterior = inicio.AddDays(-1);

        // Act
        var act = () => new Campanha(titulo, "Descricao valida", meta, inicio, fimAnterior, "gestor@ong.com");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("422_DATES_MISMATCHING");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Construtor_ComCriadoPorNuloOuVazio_DeveLancarDomainException(string? solicitanteInvalido)
    {
        // Arrange
        var titulo = TestData.CriarTituloValido();
        var meta = TestData.CriarMetaValida();

        // Act
        var act = () => new Campanha(titulo, "Descricao valida", meta, DateTime.UtcNow, DateTime.UtcNow.AddDays(10), solicitanteInvalido!);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("400_REQUESTER_REQUIRED");
    }

    // -----------------------------------------------------------------------------
    // AlterarCampanha
    // -----------------------------------------------------------------------------
    [Fact]
    public void AlterarCampanha_QuandoCampanhaAtiva_DeveAtualizarPropriedades()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();
        var novoTitulo = TestData.CriarTituloValido("Novo titulo da campanha");
        var novaMeta = TestData.CriarMetaValida(20_000m);
        var novoInicio = DateTime.UtcNow.AddDays(1);
        var novoFim = novoInicio.AddDays(60);

        // Act
        campanha.AlterarCampanha(novoTitulo, "  Nova descricao  ", novaMeta, novoInicio, novoFim, "gestor2@ong.com");

        // Assert
        campanha.Titulo.Should().Be(novoTitulo);
        campanha.Descricao.Should().Be("Nova descricao"); // deve aplicar Trim
        campanha.MetaFinanceira.Should().Be(novaMeta);
        campanha.DataInicio.Should().Be(novoInicio);
        campanha.DataFim.Should().Be(novoFim);
        campanha.ModificadoPor.Should().Be("gestor2@ong.com");
        campanha.StatusCampanha.Should().Be(CampanhaStatus.ATIVA); // status nao deve mudar
    }

    [Fact]
    public void AlterarCampanha_QuandoCampanhaCancelada_DeveLancarDomainException()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();
        campanha.CancelarCampanha("gestor@ong.com");

        // Act
        var act = () => campanha.AlterarCampanha(
            TestData.CriarTituloValido(), "Nova descricao", TestData.CriarMetaValida(),
            DateTime.UtcNow, DateTime.UtcNow.AddDays(10), "gestor@ong.com");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("422_CAMPAIGN_ACTIVE_CAN_BE_EDITED");
    }

    [Fact]
    public void AlterarCampanha_QuandoCampanhaConcluida_DeveLancarDomainException()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();
        campanha.ConcluirCampanha("gestor@ong.com");

        // Act
        var act = () => campanha.AlterarCampanha(
            TestData.CriarTituloValido(), "Nova descricao", TestData.CriarMetaValida(),
            DateTime.UtcNow, DateTime.UtcNow.AddDays(10), "gestor@ong.com");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("422_CAMPAIGN_ACTIVE_CAN_BE_EDITED");
    }

    [Fact]
    public void AlterarCampanha_ComDadosInvalidos_DeveValidarAntesDeChecarStatus()
    {
        // Arrange: campanha ja cancelada, mas o foco aqui e' garantir que a validacao
        // de dados (descricao vazia) e' executada e lanca seu proprio erro.
        var campanha = TestData.CriarCampanhaValida();
        campanha.CancelarCampanha("gestor@ong.com");

        // Act
        var act = () => campanha.AlterarCampanha(
            TestData.CriarTituloValido(), "", TestData.CriarMetaValida(),
            DateTime.UtcNow, DateTime.UtcNow.AddDays(10), "gestor@ong.com");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("400_DESCRIPTION_REQUIRED");
    }

    // -----------------------------------------------------------------------------
    // CancelarCampanha
    // -----------------------------------------------------------------------------
    [Fact]
    public void CancelarCampanha_QuandoAtiva_DeveAlterarStatusParaCancelada()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();

        // Act
        campanha.CancelarCampanha("gestor@ong.com");

        // Assert
        campanha.StatusCampanha.Should().Be(CampanhaStatus.CANCELADA);
        campanha.ModificadoPor.Should().Be("gestor@ong.com");
    }

    [Fact]
    public void CancelarCampanha_QuandoJaCancelada_DeveLancarDomainException()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();
        campanha.CancelarCampanha("gestor@ong.com");

        // Act
        var act = () => campanha.CancelarCampanha("gestor@ong.com");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("422_CAMPAIGN_ALREADY_CANCELLED");
    }

    [Fact]
    public void CancelarCampanha_QuandoConcluida_DeveLancarDomainException()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();
        campanha.ConcluirCampanha("gestor@ong.com");

        // Act
        var act = () => campanha.CancelarCampanha("gestor@ong.com");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("422_CAMPAIGN_FINISHED_CANNOT_CANCEL");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CancelarCampanha_ComModificadoPorInvalido_DeveLancarDomainException(string? modificadoPorInvalido)
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();

        // Act
        var act = () => campanha.CancelarCampanha(modificadoPorInvalido!);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("400_REQUESTER_REQUIRED");
    }

    // -----------------------------------------------------------------------------
    // ConcluirCampanha
    // -----------------------------------------------------------------------------
    [Fact]
    public void ConcluirCampanha_QuandoAtiva_DeveAlterarStatusParaConcluida()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();

        // Act
        campanha.ConcluirCampanha("gestor@ong.com");

        // Assert
        campanha.StatusCampanha.Should().Be(CampanhaStatus.CONCLUIDA);
        campanha.ModificadoPor.Should().Be("gestor@ong.com");
    }

    [Fact]
    public void ConcluirCampanha_QuandoJaConcluida_DeveLancarDomainException()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();
        campanha.ConcluirCampanha("gestor@ong.com");

        // Act
        var act = () => campanha.ConcluirCampanha("gestor@ong.com");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("422_CAMPAIGN_ACTIVE_CAN_BE_FINISHED");
    }

    [Fact]
    public void ConcluirCampanha_QuandoCancelada_DeveLancarDomainException()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();
        campanha.CancelarCampanha("gestor@ong.com");

        // Act
        var act = () => campanha.ConcluirCampanha("gestor@ong.com");

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("422_CAMPAIGN_ACTIVE_CAN_BE_FINISHED");
    }

    [Fact]
    public void ConcluirCampanha_NaoValidaSolicitante_AceitaModificadoPorVazio()
    {
        // Arrange: diferente de CancelarCampanha, ConcluirCampanha nao chama
        // SolicitanteAssertions antes de validar o status. Este teste documenta
        // esse comportamento (possível inconsistência em relação aos demais metodos).
        var campanha = TestData.CriarCampanhaValida();

        // Act
        campanha.ConcluirCampanha("");

        // Assert
        campanha.StatusCampanha.Should().Be(CampanhaStatus.CONCLUIDA);
        campanha.ModificadoPor.Should().Be("");
    }
}
