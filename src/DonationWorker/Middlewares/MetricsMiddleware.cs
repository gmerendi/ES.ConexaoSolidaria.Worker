using DonationWorker.Domain.Shared.Interfaces;
using System.Diagnostics;

namespace DonationWorker.Api.Middlewares;

/// <summary>
/// Middleware que intercepta todas as requisições HTTP e registra a duração
/// no histograma do Prometheus, alimentando os percentis p90/p95/p99.
/// 
/// Deve ser registrado ANTES dos outros middlewares para capturar o tempo total.
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMetricsService _metrics;

    public MetricsMiddleware(RequestDelegate next, IMetricsService metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Ignora o próprio endpoint /metrics para não gerar ruído nas métricas
        if (context.Request.Path.StartsWithSegments("/metrics"))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();

            var metodo = context.Request.Method;
            var rota = NormalizarRota(context);
            var statusCode = context.Response.StatusCode;
            var duracao = sw.Elapsed.TotalSeconds;

            _metrics.RegistrarDuracaoRequisicao(metodo, rota, statusCode, duracao);
        }
    }

    /// <summary>
    /// Normaliza a rota substituindo segmentos variáveis (ex: GUIDs, IDs) por {id},
    /// evitando cardinalidade explosiva no Prometheus.
    /// Ex: /api/v1/usuario/abc123 → /api/v1/usuario/{id}
    /// </summary>
    private static string NormalizarRota(HttpContext context)
    {
        // Tenta usar o RoutePattern definido pelo ASP.NET (mais preciso)
        var endpoint = context.GetEndpoint();
        if (endpoint is RouteEndpoint routeEndpoint)
            return routeEndpoint.RoutePattern.RawText ?? context.Request.Path;

        return context.Request.Path;
    }
}

/// <summary>
/// Extension method para registrar o middleware no pipeline de forma fluente.
/// </summary>
public static class MetricsMiddlewareExtensions
{
    public static IApplicationBuilder UseMetricsMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<MetricsMiddleware>();
}
