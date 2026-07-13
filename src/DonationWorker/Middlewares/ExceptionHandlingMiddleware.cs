using DonationWorker.Domain.Enums;
using DonationWorker.Domain.Shared.Exceptions;
using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Domain.Shared.Resources;
using System.Net;
using System.Text.Json;

namespace DonationWorker.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IBaseLogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, IBaseLogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdGenerator correlationIdGenerator)
    {
        var correlationId = correlationIdGenerator.Get() ?? "N/A";

        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // cliente cancelou a requisição (fechou o browser, timeout, etc.)
            // não loga como erro — comportamento esperado

            _logger.LogInformation(
                                "Requisição cancelada pelo usuário",
                                BaseLogType.LOG,
                                correlationId: correlationId
                                );

            context.Response.StatusCode = 499; // Client Closed Request (Convenção nginx, AWS)
        }
        catch (DomainException ex)
        {
            _logger.LogError(
                            "Erro de negócio detectado: {ErrorCode} - {ExceptionMsg}",
                            BaseLogType.LOG,
                            ex,
                            new { ErrorCode = ex.ErrorCode },
                            correlationId
                            );

            // 1. Extrai o status code (ex: 422, 400, 403) baseado no início do ErrorCode
            var statusCode = ExtrairStatusCode(ex.ErrorCode, HttpStatusCode.UnprocessableEntity);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            // 2. Monta o objeto elaborado idêntico ao padrão RFC (ValidationProblemDetails)
            var respostaElaborada = new
            {
                title = "A domain error occurred.",
                status = (int)statusCode,
                errors = new Dictionary<string, string[]>
        {
            // Agrupa o erro na chave "Domain" para manter a estrutura de array/dicionário
            { "Domain", new[] { ex.Message } }
        },
                traceId = correlationId,
                code = ex.ErrorCode
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(respostaElaborada));
        }
        catch (BadHttpRequestException ex) // Captura falhas de validação de modelo/Data Annotations
        {
            _logger.LogWarning(
                                "Falha na validação dos dados de entrada: {ExceptionMsg}",
                                BaseLogType.LOG,
                                ex,
                                correlationId: correlationId
                                );

            // Tenta buscar uma chave do resource (ex: "400_NAME_REQUIRED")
            string errorCode = ex.Message;
            string mensagemTraduzida = ErrorMessages.GetString(errorCode);

            var statusCode = ExtrairStatusCode(errorCode, HttpStatusCode.BadRequest);

            await FormatarRespostaErroAsync(context, statusCode, errorCode, mensagemTraduzida);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                            "Ocorreu um erro não tratado no servidor: {ExceptionMsg}",
                            BaseLogType.LOG,
                            ex,
                            correlationId: correlationId
                            );

            string codigoErroInesperado = "500_ERRO_INESPERADO";
            string mensagemInesperada = ErrorMessages.GetString(codigoErroInesperado);

            await FormatarRespostaErroAsync(context, HttpStatusCode.InternalServerError, codigoErroInesperado, mensagemInesperada);
        }
    }


    private static async Task FormatarRespostaErroAsync(HttpContext context, HttpStatusCode statusCode, string errorCode, string mensagem)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var resposta = new
        {
            code = errorCode,
            error = mensagem
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(resposta));
    }

    // Método que lê o "400_" ou "422_" do início da string e converte no Enum do .NET
    private static HttpStatusCode ExtrairStatusCode(string? errorCode, HttpStatusCode fallback)
    {
        if (!string.IsNullOrWhiteSpace(errorCode) && errorCode.Length >= 4)
        {
            var tresPrimeirosCaracteres = errorCode.Substring(0, 3);
            if (int.TryParse(tresPrimeirosCaracteres, out int code))
            {
                return (HttpStatusCode)code;
            }
        }
        return fallback;
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}