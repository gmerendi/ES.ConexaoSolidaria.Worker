namespace DonationWorker.Consumers;

using CS.Domain.Events;
using DonationWorker.Domain.Entities.Campanhas;
using DonationWorker.Domain.Entities.Doacoes;
using DonationWorker.Domain.Enums;
using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Domain.ValueObjects;
using MassTransit;

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

    public DonationCreatedEventConsumer(IDoacaoRepository doacaoRepository,
        ICampanhaRepository campanhaRepository, IBaseLogger<DonationCreatedEventConsumer> logger,
        IUnitOfWork unitOfWork, IMessageService messageService, ICryptoService cryptoService)
    {
        _doacaoRepository = doacaoRepository;
        _campanhaRepository = campanhaRepository;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _cryptoService = cryptoService;
    }

    /// <summary>
    /// Método chamado quando um evento DonationCreatedEvent é recebido
    /// </summary>
    public async Task Consume(ConsumeContext<DonationCreatedEvent> context)
    {
        var donationEvent = context.Message;

        _logger.LogInformation("Evento recebido: DonationCreatedEvent", BaseLogType.EVENT, donationEvent, donationEvent.correlationId);


        try
        {
            // Processar pagamento
            _logger.LogInformation("Processando doacao.", BaseLogType.EVENT, donationEvent);

            // 1. Checa se camapanha existe
            var campanha = await _campanhaRepository.ObterPorGuidAsync(donationEvent.guidCampanha);

            if (campanha == null)
            {
                throw new ApplicationException($"Campanha nao existe com o guid: {donationEvent.guidCampanha}");
            }

            // 2. Checa se valor é  >0
            if (donationEvent.valor <= 0) 
            {
                throw new ApplicationException($"Valor de doacao deve ser maior que 0");
            }

            // 3. Checa se a doacao ja nao foi processada
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
            var doacao = new Doacao(
                donationEvent.guidUser, 
                donationEvent.nome, 
                email, 
                cpf, 
                donationEvent.guidCampanha, 
                titulo, 
                donationEvent.valor, 
                donationEvent.correlationId);

            // 4. Grava a doacao
            // Faz update na base de dados diretamente, nao faz update em campaha e guarda campanha
            // porque se houver mais de um worker, corremos o risco de perder valor devido a race condition
            await _unitOfWork.BeginTransactionAsync(context.CancellationToken);
            try
            {
                await _doacaoRepository.CadastrarAsync(doacao);
                await _campanhaRepository.ObterEAlterarAsync(campanha, donationEvent.valor);
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
                   donationEvent.correlationId,
                   context.CancellationToken               
                );
        
            _logger.LogInformation("Evento publicado: DonationProcessedEvent.",BaseLogType.EVENT, donationEvent);

        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao processar doacao com correlationId: " + donationEvent.correlationId,
                BaseLogType.EVENT, ex);
            
            // Lançar exceção para que o MassTransit tente reprocessar a mensagem
            throw;
        }
    }
}
