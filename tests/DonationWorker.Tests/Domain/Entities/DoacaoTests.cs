using DonationWorker.Domain.Entities.Doacoes;
using DonationWorker.Domain.Enums;
using DonationWorker.Domain.Shared.Exceptions;
using DonationWorker.Domain.ValueObjects;
using DonationWorker.Tests.Common;
using FluentAssertions;
using Xunit;

namespace DonationWorker.Tests.Domain.Entities;

public class DoacaoTests
{
    private static (Guid guidUsuario, string nome, Email email, Cpf cpf, Guid guidCampanha,
        TituloCampanha titulo, decimal valor, string correlationId, DoacaoStatus status) DadosValidos()
    {
        return (
            Guid.NewGuid(),
            "Joao da Silva",
            TestData.CriarEmailValido(),
            TestData.CriarCpfValido(),
            Guid.NewGuid(),
            TestData.CriarTituloValido(),
            100m,
            Guid.NewGuid().ToString(),
            DoacaoStatus.PROCESSANDO);
    }

    [Fact]
    public void Construtor_ComDadosValidos_DeveCriarDoacaoComPropriedadesCorretas()
    {
        // Arrange
        var (guidUsuario, nome, email, cpf, guidCampanha, titulo, valor, correlationId, status) = DadosValidos();

        // Act
        var doacao = new Doacao(guidUsuario, nome, email, cpf, guidCampanha, titulo, valor, correlationId, status);

        // Assert
        doacao.GuidUsuario.Should().Be(guidUsuario);
        doacao.NomeUsuario.Should().Be(nome);
        doacao.EmailUsuario.Should().Be(email);
        doacao.CpfUsuario.Should().Be(cpf);
        doacao.GuidCampanha.Should().Be(guidCampanha);
        doacao.TituloCampanha.Should().Be(titulo);
        doacao.ValorDoacao.Should().Be(valor);
        doacao.CorrelationId.Should().Be(correlationId);
        doacao.StatusDoacao.Should().Be(status);
        doacao.CriadoPor.Should().Be("Worker");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Construtor_ComNomeNuloOuVazio_DeveLancarDomainException(string? nomeInvalido)
    {
        // Arrange
        var (guidUsuario, _, email, cpf, guidCampanha, titulo, valor, correlationId, status) = DadosValidos();

        // Act
        var act = () => new Doacao(guidUsuario, nomeInvalido!, email, cpf, guidCampanha, titulo, valor, correlationId, status);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("400_NAME_REQUIRED");
    }

    [Theory]
    [InlineData("abcd")] // 4 caracteres, abaixo do minimo de 5
    [InlineData("Jo")]
    public void Construtor_ComNomeMenorQueTamanhoMinimo_DeveLancarDomainException(string nomeCurto)
    {
        // Arrange
        var (guidUsuario, _, email, cpf, guidCampanha, titulo, valor, correlationId, status) = DadosValidos();

        // Act
        var act = () => new Doacao(guidUsuario, nomeCurto, email, cpf, guidCampanha, titulo, valor, correlationId, status);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("400_NAME_LENGTH_INVALID");
    }

    [Fact]
    public void Construtor_ComNomeMaiorQue200Caracteres_DeveLancarDomainException()
    {
        // Arrange
        var (guidUsuario, _, email, cpf, guidCampanha, titulo, valor, correlationId, status) = DadosValidos();
        var nomeLongo = new string('a', 201);

        // Act
        var act = () => new Doacao(guidUsuario, nomeLongo, email, cpf, guidCampanha, titulo, valor, correlationId, status);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("400_NAME_LENGTH_INVALID");
    }

    [Fact]
    public void Construtor_ComEmailNulo_DeveLancarDomainException()
    {
        // Arrange
        var (guidUsuario, nome, _, cpf, guidCampanha, titulo, valor, correlationId, status) = DadosValidos();

        // Act
        var act = () => new Doacao(guidUsuario, nome, null!, cpf, guidCampanha, titulo, valor, correlationId, status);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("400_EMAIL_REQUIRED");
    }

    [Fact]
    public void Construtor_ComCpfNulo_DeveLancarDomainException()
    {
        // Arrange
        var (guidUsuario, nome, email, _, guidCampanha, titulo, valor, correlationId, status) = DadosValidos();

        // Act
        var act = () => new Doacao(guidUsuario, nome, email, null!, guidCampanha, titulo, valor, correlationId, status);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("400_CPF_REQUIRED");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Construtor_ComValorDoacaoMenorOuIgualAZero_DeveLancarDomainException(decimal valorInvalido)
    {
        // Arrange
        var (guidUsuario, nome, email, cpf, guidCampanha, titulo, _, correlationId, status) = DadosValidos();

        // Act
        var act = () => new Doacao(guidUsuario, nome, email, cpf, guidCampanha, titulo, valorInvalido, correlationId, status);

        // Assert
        // Nota: a entidade reutiliza (por engano, aparentemente um copy-paste no codigo
        // original) o codigo de erro "400_TITLE_LENGTH_INVALID" para valor invalido,
        // em vez de um codigo proprio para valor. O teste documenta o comportamento atual.
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("400_TITLE_LENGTH_INVALID");
    }

    [Fact]
    public void Construtor_ComTituloNulo_DeveLancarNullReferenceException()
    {
        // Arrange
        var (guidUsuario, nome, email, cpf, guidCampanha, _, valor, correlationId, status) = DadosValidos();

        // Act
        var act = () => new Doacao(guidUsuario, nome, email, cpf, guidCampanha, null!, valor, correlationId, status);

        // Assert
        // Nota: TituloAssertions acessa "titulo.Valor" sem checar nulidade antes,
        // diferentemente de EmailAssertions/CpfAssertions (que usam AssertArgumentNotNull).
        // Por isso o comportamento atual e' NullReferenceException, e nao DomainException.
        // Este teste documenta o comportamento real do codigo; se a checagem de nulidade
        // for adicionada futuramente (recomendado), este teste deve ser atualizado.
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void Construtor_ComValorDoacaoPositivo_DeveCriarComSucesso()
    {
        // Arrange
        var (guidUsuario, nome, email, cpf, guidCampanha, titulo, _, correlationId, status) = DadosValidos();

        // Act
        var doacao = new Doacao(guidUsuario, nome, email, cpf, guidCampanha, titulo, 0.01m, correlationId, status);

        // Assert
        doacao.ValorDoacao.Should().Be(0.01m);
    }
}