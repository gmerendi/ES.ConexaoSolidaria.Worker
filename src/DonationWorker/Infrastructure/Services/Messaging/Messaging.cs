using Amazon.SQS;
using Amazon.SQS.Model;
using CS.Domain.Events;
using DonationWorker.Domain.Enums;
using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Domain.ValueObjects;
using MassTransit;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace DonationWorker.Infrastructure.Services.Messaging
{
    public class MessageService : IMessageService
    {
        private readonly IPublishEndpoint _publish;
        private readonly string _donationProcessedQueueUrl;
        private readonly string _applicationType;
        private readonly IBaseLogger<MessageService> _logger;
        private readonly ICorrelationIdGenerator _correlationIdGenerator;
        private readonly IAmazonSQS _sqsClient;



        public MessageService(IPublishEndpoint publish, IConfiguration configuration,
            IBaseLogger<MessageService> logger, ICorrelationIdGenerator correlationIdGenerator,
            IAmazonSQS sqsClient)
        {
            _publish = publish;
            _donationProcessedQueueUrl = Environment.GetEnvironmentVariable("DONATION_PROCESSED_QUEUE")
                                   ?? configuration["DONATION_PROCESSED_QUEUE"]
                                   ?? "donation-processed-queue-failed";
            _applicationType = Environment.GetEnvironmentVariable("Application__Type")
                                   ?? configuration["Application__Type"]
                                   ?? "application_type_failed";
            _correlationIdGenerator = correlationIdGenerator;
            _logger = logger;
            _sqsClient = sqsClient;
        }



        public async Task SendDonationProcessedEventMessage(Guid guidUser, string nome, string email, Guid guidCampanha, string tituloCampanha, decimal valor, string correlationId, CancellationToken ct)
        {

            if (_applicationType == "LOCAL")
            {
                await SendDonationProcessedEventMessageRabbit(guidUser, nome, email, guidCampanha, tituloCampanha, valor, correlationId, ct);
            }
            else if (_applicationType == "LAB")
            {
                await SendDonationProcessedEventMessageSQS(guidUser, nome, email, guidCampanha, tituloCampanha, valor, correlationId, ct);
            }

        }




        // -----------------------------------------------------------------------------
        // Privados
        // -----------------------------------------------------------------------------
        private async Task SendDonationProcessedEventMessageRabbit(Guid guidUser, string nome, string email, Guid guidCampanha, string tituloCampanha, decimal valor, string correlationId, CancellationToken ct)
        {

            try
            {
                var eventMessage = new DonationProcessedEvent(guidUser, nome, email, guidCampanha, tituloCampanha, valor, (correlationId ?? _correlationIdGenerator.Get()));
                await _publish.Publish(eventMessage, ct);
                _logger.LogInformation("Evento DonationProcessedEvent publicado para o Broker. Email: " + email, BaseLogType.EVENT, eventMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao publicar evento DonationProcessedEvent para o Broker : " + email, BaseLogType.EVENT, ex);
                throw;
            }

        }





        private async Task SendDonationProcessedEventMessageSQS(Guid guidUser, string nome, string email, Guid guidCampanha, string tituloCampanha, decimal valor, string correlationId, CancellationToken ct)
        {
            _logger.LogInformation("Evento DonationProcessedEvent iniciado para a fila: " + _donationProcessedQueueUrl, BaseLogType.EVENT, null);
            var message = new
            {
                guidUser = guidUser.ToString(),
                nome = nome,
                email = email,
                guidCampanha = guidCampanha.ToString(),
                tituloCampanha = tituloCampanha,
                valor = valor,
                correlationId = _correlationIdGenerator.Get()
            };

            try
            {
                var messageBody = JsonSerializer.Serialize(message);
                var response = await _sqsClient.SendMessageAsync(new SendMessageRequest
                {
                    QueueUrl = _donationProcessedQueueUrl,
                    MessageBody = messageBody
                });
                _logger.LogInformation("Evento DonationProcessedEvent publicado para o SQS. Email: " + email, BaseLogType.EVENT, message);
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao publicar evento DonationProcessedEvent para o SQS : " + email, BaseLogType.EVENT, ex);
                throw;
            }

        }

    }
}
