using CS.Domain.Events;
using DonationWorker.Consumers;
using DonationWorker.Domain.Entities.Campanhas;
using DonationWorker.Domain.Entities.Doacoes;
using DonationWorker.Domain.Enums;
using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Tests.Common;
using FluentAssertions;
using MassTransit;
using Moq;
using System.Timers;
using Xunit;

namespace DonationWorker.Tests.Consumers;

public class DonationCreatedEventConsumerTests
{
    private readonly Mock<IDoacaoRepository> _doacaoRepositoryMock = new();
    private readonly Mock<ICampanhaRepository> _campanhaRepositoryMock = new();
    private readonly Mock<IBaseLogger<DonationCreatedEventConsumer>> _loggerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMessageService> _messageServiceMock = new();
    private readonly Mock<ICryptoService> _cryptoServiceMock = new();
    private readonly Mock<IMetricsService> _metricsServiceMock = new();

    private DonationCreatedEventConsumer CriarConsumer() => new(
        _doacaoRepositoryMock.Object,
        _campanhaRepositoryMock.Object,
        _loggerMock.Object,
        _unitOfWorkMock.Object,
        _messageServiceMock.Object,
        _cryptoServiceMock.Object,
        _metricsServiceMock.Object
        );

    /// <summary>
    /// Cria um DonationCreatedEvent valido. O campo "cpf" carrega o valor "criptografado"
    /// (no mock, e' apenas um texto qualquer), que sera' decifrado via ICryptoService.Decrypt
    /// para o CPF real (TestData.CpfValido1) usado na construcao da entidade Doacao.
    /// </summary>
    private static DonationCreatedEvent CriarEventoValido(
        Guid? guidCampanha = null,
        decimal valor = 100m,
        string correlationId = "11111111-1111-1111-1111-111111111111")
    {
        return new DonationCreatedEvent(
            guidUser: Guid.NewGuid(),
            nome: "Joao da Silva",
            email: TestData.EmailValido,
            guidCampanha: guidCampanha ?? Guid.NewGuid(),
            tituloCampanha: "Campanha de inverno solidario",
            cpf: "cpf-criptografado-base64",
            valor: valor,
            correlationId: correlationId);
    }

    private Mock<ConsumeContext<DonationCreatedEvent>> CriarContextoMock(
        DonationCreatedEvent evento, CancellationToken cancellationToken = default)
    {
        var contextMock = new Mock<ConsumeContext<DonationCreatedEvent>>();
        contextMock.SetupGet(c => c.Message).Returns(evento);
        contextMock.SetupGet(c => c.CancellationToken).Returns(cancellationToken);
        return contextMock;
    }

    // -----------------------------------------------------------------------------
    // Cenario de sucesso
    // -----------------------------------------------------------------------------
    [Fact]
    public async Task Consume_ComEventoValidoECampanhaExistente_DeveProcessarDoacaoComSucesso()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();
        var evento = CriarEventoValido(guidCampanha: campanha.Guid, valor: 250m);
        var contextMock = CriarContextoMock(evento);

        _campanhaRepositoryMock
            .Setup(r => r.ObterPorGuidAsync(evento.guidCampanha, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campanha);

        _doacaoRepositoryMock
            .Setup(r => r.ObterPorCorrelationIdAsync(evento.correlationId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doacao?)null);

        _cryptoServiceMock
            .Setup(c => c.Decrypt(evento.cpf))
            .Returns(TestData.CpfValido1);

        var consumer = CriarConsumer();

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        _campanhaRepositoryMock.Verify(r => r.ObterPorGuidAsync(evento.guidCampanha, It.IsAny<CancellationToken>()), Times.Once);
        _doacaoRepositoryMock.Verify(r => r.ObterPorCorrelationIdAsync(evento.correlationId!, It.IsAny<CancellationToken>()), Times.Once);
        _cryptoServiceMock.Verify(c => c.Decrypt(evento.cpf), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _doacaoRepositoryMock.Verify(r => r.CadastrarAsync(
            It.Is<Doacao>(d =>
                d.GuidUsuario == evento.guidUser &&
                d.NomeUsuario == evento.nome &&
                d.GuidCampanha == evento.guidCampanha &&
                d.ValorDoacao == evento.valor &&
                d.CorrelationId == evento.correlationId &&
                d.CpfUsuario.Numero == TestData.CpfValido1 &&
                d.EmailUsuario.Endereco == evento.email),
            It.IsAny<CancellationToken>()), Times.Once);

        _campanhaRepositoryMock.Verify(r => r.ObterEAlterarAsync(campanha, evento.valor, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);

        _messageServiceMock.Verify(m => m.SendDonationProcessedEventMessage(
            evento.guidUser, evento.nome, evento.email, evento.guidCampanha,
            evento.tituloCampanha, evento.valor, evento.correlationId!, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_ComSucesso_DeveLogarRecebimentoEPublicacaoDoEvento()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();
        var evento = CriarEventoValido(guidCampanha: campanha.Guid);
        var contextMock = CriarContextoMock(evento);

        _campanhaRepositoryMock
            .Setup(r => r.ObterPorGuidAsync(evento.guidCampanha, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campanha);
        _doacaoRepositoryMock
            .Setup(r => r.ObterPorCorrelationIdAsync(evento.correlationId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doacao?)null);
        _cryptoServiceMock.Setup(c => c.Decrypt(evento.cpf)).Returns(TestData.CpfValido1);

        var consumer = CriarConsumer();

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert: ao menos uma chamada de log informativo mencionando o recebimento do evento
        _loggerMock.Verify(l => l.LogInformation(
            It.Is<string>(msg => msg.Contains("Evento recebido")),
            BaseLogType.EVENT, It.IsAny<object?>(), evento.correlationId), Times.Once);

        _loggerMock.Verify(l => l.LogInformation(
            It.Is<string>(msg => msg.Contains("Evento publicado")),
            BaseLogType.EVENT, It.IsAny<object?>(), null), Times.Once);

        _loggerMock.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<BaseLogType>(), It.IsAny<object?>(), It.IsAny<string?>()), Times.Never);
    }

    // -----------------------------------------------------------------------------
    // Campanha inexistente
    // -----------------------------------------------------------------------------
    [Fact]
    public async Task Consume_QuandoCampanhaNaoExiste_DeveLancarApplicationExceptionENaoIniciarTransacao()
    {
        // Arrange
        var evento = CriarEventoValido();
        var contextMock = CriarContextoMock(evento);

        _campanhaRepositoryMock
            .Setup(r => r.ObterPorGuidAsync(evento.guidCampanha, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Campanha?)null);

        var consumer = CriarConsumer();

        // Act
        var act = async () => await consumer.Consume(contextMock.Object);

        // Assert
        var assertion = await act.Should().ThrowAsync<ApplicationException>();
        assertion.Which.Message.Should().Contain(evento.guidCampanha.ToString());

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _doacaoRepositoryMock.Verify(r => r.CadastrarAsync(It.IsAny<Doacao>(), It.IsAny<CancellationToken>()), Times.Never);
        _messageServiceMock.Verify(m => m.SendDonationProcessedEventMessage(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Consume_QuandoCampanhaNaoExiste_DeveLogarErroERelancarExcecao()
    {
        // Arrange
        var evento = CriarEventoValido();
        var contextMock = CriarContextoMock(evento);

        _campanhaRepositoryMock
            .Setup(r => r.ObterPorGuidAsync(evento.guidCampanha, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Campanha?)null);

        var consumer = CriarConsumer();

        // Act
        var act = async () => await consumer.Consume(contextMock.Object);
        await act.Should().ThrowAsync<ApplicationException>();

        // Assert
        _loggerMock.Verify(l => l.LogError(
            It.Is<string>(msg => msg.Contains(evento.correlationId!)),
            BaseLogType.EVENT, It.IsAny<object?>(), It.IsAny<string?>()), Times.Once);
    }

    // -----------------------------------------------------------------------------
    // Valor invalido
    // -----------------------------------------------------------------------------
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public async Task Consume_ComValorMenorOuIgualAZero_DeveLancarApplicationException(decimal valorInvalido)
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();
        var evento = CriarEventoValido(guidCampanha: campanha.Guid, valor: valorInvalido);
        var contextMock = CriarContextoMock(evento);

        _campanhaRepositoryMock
            .Setup(r => r.ObterPorGuidAsync(evento.guidCampanha, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campanha);

        var consumer = CriarConsumer();

        // Act
        var act = async () => await consumer.Consume(contextMock.Object);

        // Assert
        await act.Should().ThrowAsync<ApplicationException>()
            .WithMessage("*maior que 0*");

        _doacaoRepositoryMock.Verify(r => r.ObterPorCorrelationIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -----------------------------------------------------------------------------
    // Idempotencia: doacao ja processada
    // -----------------------------------------------------------------------------
    [Fact]
    public async Task Consume_QuandoDoacaoJaExisteParaOCorrelationId_DeveLancarApplicationExceptionENaoDuplicar()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();
        var evento = CriarEventoValido(guidCampanha: campanha.Guid);
        var contextMock = CriarContextoMock(evento);

        var doacaoExistente = new Doacao(
            Guid.NewGuid(), "Maria Existente", TestData.CriarEmailValido(), TestData.CriarCpfValido(),
            campanha.Guid, TestData.CriarTituloValido(), 50m, evento.correlationId!);

        _campanhaRepositoryMock
            .Setup(r => r.ObterPorGuidAsync(evento.guidCampanha, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campanha);

        _doacaoRepositoryMock
            .Setup(r => r.ObterPorCorrelationIdAsync(evento.correlationId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doacaoExistente);

        var consumer = CriarConsumer();

        // Act
        var act = async () => await consumer.Consume(contextMock.Object);

        // Assert
        await act.Should().ThrowAsync<ApplicationException>()
            .WithMessage("*já inserida*");

        _cryptoServiceMock.Verify(c => c.Decrypt(It.IsAny<string>()), Times.Never);
        _doacaoRepositoryMock.Verify(r => r.CadastrarAsync(It.IsAny<Doacao>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _messageServiceMock.Verify(m => m.SendDonationProcessedEventMessage(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -----------------------------------------------------------------------------
    // Falha durante a transacao -> rollback
    // -----------------------------------------------------------------------------
    [Fact]
    public async Task Consume_QuandoCadastrarDoacaoFalha_DeveFazerRollbackERelancarExcecao()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();
        var evento = CriarEventoValido(guidCampanha: campanha.Guid);
        var contextMock = CriarContextoMock(evento);
        var excecaoOriginal = new InvalidOperationException("falha simulada ao cadastrar doacao");

        _campanhaRepositoryMock
            .Setup(r => r.ObterPorGuidAsync(evento.guidCampanha, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campanha);
        _doacaoRepositoryMock
            .Setup(r => r.ObterPorCorrelationIdAsync(evento.correlationId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doacao?)null);
        _cryptoServiceMock.Setup(c => c.Decrypt(evento.cpf)).Returns(TestData.CpfValido1);
        _doacaoRepositoryMock
            .Setup(r => r.CadastrarAsync(It.IsAny<Doacao>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(excecaoOriginal);

        var consumer = CriarConsumer();

        // Act
        var act = async () => await consumer.Consume(contextMock.Object);

        // Assert
        var assertion = await act.Should().ThrowAsync<InvalidOperationException>();
        assertion.Which.Should().BeSameAs(excecaoOriginal);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _messageServiceMock.Verify(m => m.SendDonationProcessedEventMessage(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Consume_QuandoAtualizarCampanhaFalha_DeveFazerRollbackERelancarExcecao()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();
        var evento = CriarEventoValido(guidCampanha: campanha.Guid);
        var contextMock = CriarContextoMock(evento);
        var excecaoOriginal = new InvalidOperationException("falha simulada ao atualizar campanha");

        _campanhaRepositoryMock
            .Setup(r => r.ObterPorGuidAsync(evento.guidCampanha, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campanha);
        _doacaoRepositoryMock
            .Setup(r => r.ObterPorCorrelationIdAsync(evento.correlationId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doacao?)null);
        _cryptoServiceMock.Setup(c => c.Decrypt(evento.cpf)).Returns(TestData.CpfValido1);
        _campanhaRepositoryMock
            .Setup(r => r.ObterEAlterarAsync(campanha, evento.valor, It.IsAny<CancellationToken>()))
            .ThrowsAsync(excecaoOriginal);

        var consumer = CriarConsumer();

        // Act
        var act = async () => await consumer.Consume(contextMock.Object);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        _doacaoRepositoryMock.Verify(r => r.CadastrarAsync(It.IsAny<Doacao>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -----------------------------------------------------------------------------
    // Falha ao publicar evento apos commit
    // -----------------------------------------------------------------------------
    [Fact]
    public async Task Consume_QuandoPublicacaoDoEventoFalha_DeveRelancarExcecaoMasNaoFazerRollback()
    {
        // Arrange: o commit ja' ocorreu antes da publicacao, entao uma falha ao publicar
        // nao deve (e nao pode, segundo o codigo atual) desfazer a transacao ja commitada.
        var campanha = TestData.CriarCampanhaValida();
        var evento = CriarEventoValido(guidCampanha: campanha.Guid);
        var contextMock = CriarContextoMock(evento);
        var excecaoOriginal = new InvalidOperationException("falha simulada ao publicar evento");

        _campanhaRepositoryMock
            .Setup(r => r.ObterPorGuidAsync(evento.guidCampanha, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campanha);
        _doacaoRepositoryMock
            .Setup(r => r.ObterPorCorrelationIdAsync(evento.correlationId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doacao?)null);
        _cryptoServiceMock.Setup(c => c.Decrypt(evento.cpf)).Returns(TestData.CpfValido1);
        _messageServiceMock
            .Setup(m => m.SendDonationProcessedEventMessage(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(excecaoOriginal);

        var consumer = CriarConsumer();

        // Act
        var act = async () => await consumer.Consume(contextMock.Object);

        // Assert
        var assertion = await act.Should().ThrowAsync<InvalidOperationException>();
        assertion.Which.Should().BeSameAs(excecaoOriginal);

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -----------------------------------------------------------------------------
    // Email/Titulo/Cpf invalidos vindos do evento
    // -----------------------------------------------------------------------------
    [Fact]
    public async Task Consume_ComEmailInvalidoNoEvento_DeveLancarDomainExceptionENaoIniciarTransacao()
    {
        // Arrange
        var campanha = TestData.CriarCampanhaValida();
        var evento = new DonationCreatedEvent(
            guidUser: Guid.NewGuid(), nome: "Joao da Silva", email: "email-invalido-sem-arroba",
            guidCampanha: campanha.Guid, tituloCampanha: "Campanha valida",
            cpf: "cpf-criptografado", valor: 100m, correlationId: Guid.NewGuid().ToString());
        var contextMock = CriarContextoMock(evento);

        _campanhaRepositoryMock
            .Setup(r => r.ObterPorGuidAsync(evento.guidCampanha, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campanha);
        _doacaoRepositoryMock
            .Setup(r => r.ObterPorCorrelationIdAsync(evento.correlationId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doacao?)null);
        _cryptoServiceMock.Setup(c => c.Decrypt(evento.cpf)).Returns(TestData.CpfValido1);

        var consumer = CriarConsumer();

        // Act
        var act = async () => await consumer.Consume(contextMock.Object);

        // Assert
        await act.Should().ThrowAsync<DonationWorker.Domain.Shared.Exceptions.DomainException>()
            .Where(ex => ex.ErrorCode == "422_EMAIL_INVALID_FORMAT");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_ComCpfDescriptografadoInvalido_DeveLancarDomainExceptionENaoIniciarTransacao()
    {
        // Arrange: o CryptoService retorna um CPF que nao passa na validacao de Cpf.Create
        var campanha = TestData.CriarCampanhaValida();
        var evento = CriarEventoValido(guidCampanha: campanha.Guid);
        var contextMock = CriarContextoMock(evento);

        _campanhaRepositoryMock
            .Setup(r => r.ObterPorGuidAsync(evento.guidCampanha, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campanha);
        _doacaoRepositoryMock
            .Setup(r => r.ObterPorCorrelationIdAsync(evento.correlationId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doacao?)null);
        _cryptoServiceMock.Setup(c => c.Decrypt(evento.cpf)).Returns(TestData.CpfDigitosInvalidos);

        var consumer = CriarConsumer();

        // Act
        var act = async () => await consumer.Consume(contextMock.Object);

        // Assert
        await act.Should().ThrowAsync<DonationWorker.Domain.Shared.Exceptions.DomainException>()
            .Where(ex => ex.ErrorCode == "422_CPF_INVALID");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -----------------------------------------------------------------------------
    // Garantia de uso do CancellationToken do contexto
    // -----------------------------------------------------------------------------
    [Fact]
    public async Task Consume_DevePropagarCancellationTokenDoContextoParaUnitOfWorkEMessageService()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var campanha = TestData.CriarCampanhaValida();
        var evento = CriarEventoValido(guidCampanha: campanha.Guid);
        var contextMock = CriarContextoMock(evento, token);

        _campanhaRepositoryMock
            .Setup(r => r.ObterPorGuidAsync(evento.guidCampanha, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campanha);
        _doacaoRepositoryMock
            .Setup(r => r.ObterPorCorrelationIdAsync(evento.correlationId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doacao?)null);
        _cryptoServiceMock.Setup(c => c.Decrypt(evento.cpf)).Returns(TestData.CpfValido1);

        var consumer = CriarConsumer();

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(token), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(token), Times.Once);
        _messageServiceMock.Verify(m => m.SendDonationProcessedEventMessage(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), token), Times.Once);
    }
}
