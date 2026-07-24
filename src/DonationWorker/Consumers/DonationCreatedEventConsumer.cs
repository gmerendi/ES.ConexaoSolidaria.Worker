namespace DonationWorker.Consumers;

using CS.Domain.Events;
using DonationWorker.Domain.Entities.Campanhas;
using DonationWorker.Domain.Entities.Doacoes;
using DonationWorker.Domain.Enums;
using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Domain.ValueObjects;
using MassTransit;
using System.Diagnostics;

/// <summary>
/// Consumer responsável por processar eventos de pedidos realizados
/// </summary>
public class DonationCreatedEventConsumer : IConsumer<DonationCreatedEvent>
{
    private readonly IDoacaoRepository _doacaoRepository;
    private readonly ICampanhaRepository _campanhaRepository;
    private readonly IBaseLogger<DonationCreatedEventConsumer> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ICryptoService _cryptoService;
    private readonly IMetricsService _metrics;

    public DonationCreatedEventConsumer(IDoacaoRepository doacaoRepository,
        ICampanhaRepository campanhaRepository, IBaseLogger<DonationCreatedEventConsumer> logger,
        IUnitOfWork unitOfWork, IMessageService messageService, ICryptoService cryptoService,
        IMetricsService metrics)
    {
        _doacaoRepository = doacaoRepository;
        _campanhaRepository = campanhaRepository;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _cryptoService = cryptoService;
        _metrics = metrics;
    }

    /// <summary>
    /// Método chamado quando um evento DonationCreatedEvent é recebido
    /// </summary>
    public async Task Consume(ConsumeContext<DonationCreatedEvent> context)
    {
        var sw = Stopwatch.StartNew();
        var sucesso = false;
        var donationEvent = context.Message;

        _logger.LogInformation("Evento recebido: DonationCreatedEvent", BaseLogType.EVENT, donationEvent, donationEvent.correlationId);


        try
        {
            // Processar pagamento
            _logger.LogInformation("Processando doacao do usuario {guidUser} para campanha: {tituloCampanha}", BaseLogType.EVENT, donationEvent.guidUser, donationEvent.correlationId);

            // 1. Checa se camapanha existe.  caso nao exista, publica evento de doacao processada com status recusada
            var campanha = await _campanhaRepository.ObterPorGuidAsync(donationEvent.guidCampanha);

            if (campanha == null)
            {
                _logger.LogInformation("Campanha nao existe com o guid: {guidCampanha}", BaseLogType.EVENT, donationEvent.guidCampanha);
                await _messageService.SendDonationProcessedEventMessage(
                    donationEvent.guidUser,
                    donationEvent.nome,
                    donationEvent.email,
                    donationEvent.guidCampanha,
                    donationEvent.tituloCampanha,
                    donationEvent.valor,
                    DoacaoStatus.RECUSADA.ToString(),
                    donationEvent.correlationId,                   
                    context.CancellationToken
                );
                _logger.LogInformation("Evento publicado: DonationProcessedEvent. Doacao recusada - Campanha nao xiste", BaseLogType.EVENT, donationEvent, donationEvent.correlationId);
                return;
            }

            // 2. Checa se valor é  >0.  caso seja, publica evento de doacao processada com status recusada
            if (donationEvent.valor <= 0)
            {
                _logger.LogInformation("Valor da campanha: {guidCampanha} deve ser maior que 0", BaseLogType.EVENT, donationEvent.guidCampanha);
                await _messageService.SendDonationProcessedEventMessage(
                    donationEvent.guidUser,
                    donationEvent.nome,
                    donationEvent.email,
                    donationEvent.guidCampanha,
                    donationEvent.tituloCampanha,
                    donationEvent.valor,
                    DoacaoStatus.RECUSADA.ToString(),
                    donationEvent.correlationId,
                    context.CancellationToken
                );
                _logger.LogInformation("Evento publicado: DonationProcessedEvent. Doacao recusada - Valor <= 0", BaseLogType.EVENT, donationEvent, donationEvent.correlationId);
                return;
            }

            // 3. Checa o status da doacao. (Simulação)
            // Caso o status seja diferente de aprovado, não salva no banco e publica na fila de processada
            // porém com status de recusada. 
            //if (donationEvent.status != DoacaoStatus.APROVADA.ToString())
            //{
            //    _logger.LogInformation("Status da campanha: {guidCampanha} deve ser aprovada", BaseLogType.EVENT, donationEvent.guidCampanha);
            //    await _messageService.SendDonationProcessedEventMessage(
            //        donationEvent.guidUser,
            //        donationEvent.nome,
            //        donationEvent.email,
            //        donationEvent.guidCampanha,
            //        donationEvent.tituloCampanha,
            //        donationEvent.valor,
            //        DoacaoStatus.RECUSADA.ToString(),
            //        donationEvent.correlationId,
            //        context.CancellationToken
            //    );
            //    _logger.LogInformation("Evento publicado: DonationProcessedEvent. Doacao recusada - status recusada", BaseLogType.EVENT, donationEvent, donationEvent.correlationId);
            //    return;
            //}

            // 4. Checa se a doacao ja nao foi processada
            // Idempotencia. Se o worker cair e o rabbit reenviar a  mensagem, verifica se nao foi processada
            // Ou teriamos o valor duplicado
            var doacaoExistente = await _doacaoRepository.ObterPorCorrelationIdAsync(donationEvent.correlationId);

            if (doacaoExistente != null)
            {
                throw new ApplicationException($"Doacao já inserida no banco com correlationId = {donationEvent.correlationId}");
            }

            var decryptedCpf = _cryptoService.Decrypt(donationEvent.cpf);
            var email = Email.Create(donationEvent.email);
            var titulo = TituloCampanha.Create(donationEvent.tituloCampanha);
            var cpf = Cpf.Create(decryptedCpf);
            var statusEnum = Enum.Parse<DoacaoStatus>(donationEvent.status.ToString(), ignoreCase: true);
            var doacao = new Doacao(
                donationEvent.guidUser,
                donationEvent.nome,
                email,
                cpf,
                donationEvent.guidCampanha,
                titulo,
                donationEvent.valor,
                donationEvent.correlationId,
                statusEnum);

            // 4. Grava a doacao
            // Faz update na base de dados diretamente, gravando a doacao e atualizando o valor arrecadado da campanha
            // Caso status seja APROVADA.
            // porque se houver mais de um worker, corremos o risco de perder valor devido a race condition
            await _unitOfWork.BeginTransactionAsync(context.CancellationToken);
            try
            {
                await _doacaoRepository.CadastrarAsync(doacao);
                if (donationEvent.status == DoacaoStatus.APROVADA.ToString())
                {
                    await _campanhaRepository.ObterEAlterarAsync(campanha, donationEvent.valor);
                    _logger.LogInformation("Evento publicado: DonationProcessedEvent. Doacao recusada - status recusada", BaseLogType.EVENT, donationEvent, donationEvent.correlationId);
                }
                
                await _unitOfWork.CommitAsync(context.CancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(context.CancellationToken);
                throw;
            }

            // 5. Publicar evento de pagamento processado              
            await _messageService.SendDonationProcessedEventMessage(
                   donationEvent.guidUser,
                   donationEvent.nome,
                   donationEvent.email,
                   donationEvent.guidCampanha,
                   donationEvent.tituloCampanha,
                   donationEvent.valor,
                   donationEvent.status.ToString(),
                   donationEvent.correlationId,
                   context.CancellationToken
                );

            // ── Métrica de negócio ─────────────────────────────────────────
            if (donationEvent.status != DoacaoStatus.APROVADA.ToString())
            {
                _metrics.IncrementarDoacao();
            }
                
            sucesso = true;

            _logger.LogInformation("Evento publicado: DonationProcessedEvent.", BaseLogType.EVENT, donationEvent, donationEvent.correlationId);

        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao processar doacao com correlationId: {correlationId}", BaseLogType.EVENT, ex, donationEvent, donationEvent.correlationId);

            // Lançar exceção para que o MassTransit tente reprocessar a mensagem
            throw;
        }
        finally
        {
            sw.Stop();
            _metrics.RegistrarDuracaoProcessamentoMensagem(
                nameof(DonationCreatedEvent), sucesso, sw.Elapsed.TotalSeconds);
        }
    }
}