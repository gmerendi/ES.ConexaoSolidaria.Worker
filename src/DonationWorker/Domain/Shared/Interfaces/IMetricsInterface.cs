namespace DonationWorker.Domain.Shared.Interfaces;

public interface IMetricsService
{
    // ── Contadores de negócio ─────────────────────────────────────────────────
    void IncrementarDoacao();
  

    // ── Latência de requisições HTTP (para p90/p95/p99) ───────────────────────
    void RegistrarDuracaoRequisicao(string metodo, string rota, int statusCode, double duracaoSegundos);


    // ── Latência de processamento de mensagens (RabbitMQ/MassTransit) ─────
    void RegistrarDuracaoProcessamentoMensagem(string evento, bool sucesso, double duracaoSegundos);
}
