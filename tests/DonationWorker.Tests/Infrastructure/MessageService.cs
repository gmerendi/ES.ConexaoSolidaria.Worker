using CS.Domain.Events;
using DonationWorker.Domain.Enums;
using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Infrastructure.Services.Messaging;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace DonationWorker.Tests.Infrastructure.Services;

public class MessageServiceTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private readonly Mock<IBaseLogger<MessageService>> _loggerMock = new();
    private readonly Mock<ICorrelationIdGenerator> _correlationIdGeneratorMock = new();

    private MessageService CriarServico()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new MessageService(
            _publishEndpointMock.Object,
            configuration,
            _loggerMock.Object,
            _correlationIdGeneratorMock.Object);
    }

    [Fact]
    public async Task SendDonationProcessedEventMessage_ComDadosValidos_DevePublicarEventoComOsMesmosDados()
    {
        // Arrange
        var servico = CriarServico();
        var guidUser = Guid.NewGuid();
        var guidCampanha = Guid.NewGuid();
        const string correlationId = "correlation-id-123";

        // Act
        await servico.SendDonationProcessedEventMessage(
            guidUser, "Joao da Silva", "joao@exemplo.com", guidCampanha,
            "Campanha de inverno", 150m, correlationId, CancellationToken.None);

        // Assert
        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<DonationProcessedEvent>(e =>
                e.guidUser == guidUser &&
                e.nome == "Joao da Silva" &&
                e.email == "joao@exemplo.com" &&
                e.guidCampanha == guidCampanha &&
                e.tituloCampanha == "Campanha de inverno" &&
                e.valor == 150m &&
                e.correlationId == correlationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendDonationProcessedEventMessage_DevePropagarCancellationTokenInformado()
    {
        // Arrange
        var servico = CriarServico();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await servico.SendDonationProcessedEventMessage(
            Guid.NewGuid(), "Joao da Silva", "joao@exemplo.com", Guid.NewGuid(),
            "Campanha de inverno", 100m, "correlation-id", token);

        // Assert
        _publishEndpointMock.Verify(p => p.Publish(It.IsAny<DonationProcessedEvent>(), token), Times.Once);
    }

    [Fact]
    public async Task SendDonationProcessedEventMessage_ComCorrelationIdNulo_DeveUsarValorDoCorrelationIdGenerator()
    {
        // Arrange: quando correlationId e' nulo, o servico usa o fallback do
        // ICorrelationIdGenerator.Get() para preencher o evento publicado.
        var servico = CriarServico();
        const string correlationIdGerado = "correlation-id-gerado-pelo-generator";
        _correlationIdGeneratorMock.Setup(g => g.Get()).Returns(correlationIdGerado);

        // Act
        await servico.SendDonationProcessedEventMessage(
            Guid.NewGuid(), "Joao da Silva", "joao@exemplo.com", Guid.NewGuid(),
            "Campanha de inverno", 100m, null!, CancellationToken.None);

        // Assert
        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<DonationProcessedEvent>(e => e.correlationId == correlationIdGerado),
            It.IsAny<CancellationToken>()), Times.Once);
        _correlationIdGeneratorMock.Verify(g => g.Get(), Times.Once);
    }

    [Fact]
    public async Task SendDonationProcessedEventMessage_ComSucesso_DeveLogarInformacaoSemErro()
    {
        // Arrange
        var servico = CriarServico();

        // Act
        await servico.SendDonationProcessedEventMessage(
            Guid.NewGuid(), "Joao da Silva", "joao@exemplo.com", Guid.NewGuid(),
            "Campanha de inverno", 100m, "correlation-id", CancellationToken.None);

        // Assert
        _loggerMock.Verify(l => l.LogInformation(
            It.Is<string>(msg => msg.Contains("joao@exemplo.com")),
            BaseLogType.EVENT, It.IsAny<object?>(), null), Times.Once);
        _loggerMock.Verify(l => l.LogError(
            It.IsAny<string>(), It.IsAny<BaseLogType>(), It.IsAny<object?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task SendDonationProcessedEventMessage_QuandoPublishFalha_DeveLogarErroERelancarExcecao()
    {
        // Arrange
        var servico = CriarServico();
        var excecaoOriginal = new InvalidOperationException("falha simulada ao publicar no broker");
        _publishEndpointMock
            .Setup(p => p.Publish(It.IsAny<DonationProcessedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(excecaoOriginal);

        // Act
        var act = async () => await servico.SendDonationProcessedEventMessage(
            Guid.NewGuid(), "Joao da Silva", "joao@exemplo.com", Guid.NewGuid(),
            "Campanha de inverno", 100m, "correlation-id", CancellationToken.None);

        // Assert
        var assertion = await act.Should().ThrowAsync<InvalidOperationException>();
        assertion.Which.Should().BeSameAs(excecaoOriginal);

        _loggerMock.Verify(l => l.LogError(
            It.Is<string>(msg => msg.Contains("joao@exemplo.com")),
            BaseLogType.EVENT, It.IsAny<object?>(), It.IsAny<string?>()), Times.Once);
    }
}
