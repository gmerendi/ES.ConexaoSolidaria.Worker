# ES.ConexaoSolidaria.Worker

Serviço **Worker** de processamento assíncrono de doações da plataforma **Conexão Solidária**, desenvolvido para a ONG Esperança Solidária como parte do Hackathon POSTECH/FIAP.

Responsável por:
- Consumir o evento `DonationCreatedEvent`, publicado pela API de Campanhas ao registrar uma intenção de doação;
- Validar a doação (campanha existente, valor maior que zero) e garantir **idempotência** (evita duplicar doações caso a mensagem seja reprocessada);
- Persistir a doação e atualizar o valor arrecadado da campanha de forma transacional;
- Publicar o evento `DonationProcessedEvent` ao final do processamento;
- Expor métricas de negócio e de processamento de mensagens via Prometheus.

---

## Sumário
- [Arquitetura](#arquitetura)
- [Stack Tecnológica](#stack-tecnológica)
- [Fluxo de Processamento](#fluxo-de-processamento)
- [Como Rodar Localmente](#como-rodar-localmente)
- [Variáveis de Ambiente](#variáveis-de-ambiente)
- [Eventos de Domínio](#eventos-de-dominio)
- [Observabilidade](#observabilidade)
- [Testes](#testes)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Github Actions](#github-actions)

---

## Arquitetura

Este serviço é o segundo microsserviço do domínio de Campanhas/Doações da plataforma: enquanto a API (`ES.ConexaoSolidaria.Campanhas`) recebe a intenção de doação e apenas publica um evento, é o **Worker** quem efetivamente processa a doação — persistindo o registro e atualizando o valor arrecadado da campanha.

Essa separação evita que a API atualize o banco diretamente na requisição do usuário, permitindo processamento assíncrono, desacoplado e resiliente (com retry automático em caso de falha).

O Worker roda como um **BackgroundService** hospedado em um Web Host mínimo, apenas para expor os endpoints de health check e métricas — não expõe nenhuma API de negócio.

> **Importante:** este serviço compartilha o mesmo banco de dados (`cs-campanhas`) da API de Campanhas e depende da infraestrutura (RabbitMQ, PostgreSQL, Redis, DynamoDB, Elasticsearch) já estar disponível na rede `csnetwork`. Suba o `ES.ConexaoSolidaria.Campanhas` (que provisiona essa infraestrutura) antes de subir o Worker.

> O diagrama completo da arquitetura da plataforma está no repositório de infraestrutura — [https://github.com/gmerendi/ES.ConexaoSolidaria.Infra]

## Stack Tecnológica

- **.NET 8** (Worker Service / BackgroundService)
- **MassTransit** — consumo de eventos via **RabbitMQ** (local) ou **Amazon SQS** (ambiente `LAB`/AWS)
- **Entity Framework Core** + **PostgreSQL** (mesmo banco da API de Campanhas)
- **Retry automático** e **idempotência** via verificação de `correlationId`
- **Prometheus** (métricas de negócio e de processamento)
- **Docker** / **Docker Compose**
- **xUnit** (testes)

## Fluxo de Processamento

1. O `DonationCreatedEventConsumer` recebe o evento `DonationCreatedEvent`.
2. Verifica se a campanha existe.
3. Valida se o valor da doação é maior que zero.
4. Verifica idempotência: se já existe uma doação com o mesmo `correlationId`, o processamento é interrompido (evita duplicidade em caso de reentrega da mensagem).
5. Em uma transação: grava a doação e atualiza o valor arrecadado da campanha.
6. Publica o evento `DonationProcessedEvent`.
7. Registra métricas de negócio (contagem de doações) e de duração do processamento.
8. Em caso de erro, a exceção é relançada para que o MassTransit reprocesse a mensagem automaticamente (com backoff).

## Como Rodar Localmente

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Docker Desktop 4.79.0
- Infraestrutura do `ES.ConexaoSolidaria.Campanhas` já em execução (PostgreSQL, Redis, DynamoDB, Elasticsearch, RabbitMQ e a rede `csnetwork`)

### Subindo o Worker

```bash
# 1. Certifique-se de que o repositório de Campanhas já está rodando
#    (docker compose up -d --build no ES.ConexaoSolidaria.Campanhas)

# 2. Clone este repositório
git clone https://github.com/gmerendi/ES.ConexaoSolidaria.Worker.git
cd ES.ConexaoSolidaria.Worker

# 3. Suba o Worker
docker compose up -d --build

# 4. Acompanhe os logs
docker compose logs -f cs.donationworker.api
```

Serviço exposto:

| Serviço | URL/Porta |
|---|---|
| Worker (health/metrics) | http://localhost:5004 |

Para testar o fluxo completo: crie uma campanha e envie uma doação pela API de Campanhas (`POST /api/v1/doacoes`) e acompanhe os logs do Worker processando o evento e atualizando o valor arrecadado.

## Variáveis de Ambiente

Todas já vêm configuradas no `docker-compose.yml` para o ambiente local. Principais chaves:

| Variável | Descrição |
|---|---|
| `ConnectionStrings__Database` | Conexão com o PostgreSQL (mesmo banco da API de Campanhas) |
| `ConnectionStrings__AuditLog` | Endpoint do DynamoDB (auditoria) |
| `ConnectionStrings__Redis` | Conexão com o Redis |
| `ConnectionStrings__ElasticSearch` | Endpoint do Elasticsearch |
| `RabbitMq__Host` / `RabbitMq__Username` / `RabbitMq__Password` | Conexão com o RabbitMQ |
| `QUEUES__DONATION_CREATED_QUEUE` | Nome da fila de doações (usada no modo SQS/AWS) |
| `Jwt__SecretKey` / `Jwt__Issuer` / `Jwt__Audience` / `Jwt__ExpirationHours` | Configuração do JWT (deve ser a mesma chave usada pelos demais serviços) |
| `AES__KEY` | Chave usada para descriptografar dados sensíveis (ex.: CPF) recebidos no evento |
| `Application__Type` | `LOCAL` (usa RabbitMQ) ou `LAB`/`AWS` (usa SQS) |
| `Admin__Email` / `Admin__Password` | Credenciais do usuário administrador seed |

> **Atenção:** os valores no `docker-compose.yml` são apenas para desenvolvimento local. Nunca reutilize essas chaves em produção.

## Eventos de Dominio

- **Consome:** `DonationCreatedEvent` — publicado pela API de Campanhas ao registrar uma intenção de doação.
- **Publica:** `DonationProcessedEvent` — emitido após a doação ser processada com sucesso.

## Observabilidade

- Métricas expostas em `/metrics` (Prometheus), incluindo contagem de doações processadas e duração do processamento de cada mensagem.
- Health check simples disponível em `/health`.
- Este repositório não inclui a stack de Prometheus/Grafana — ela é provisionada centralmente no repositório de infraestrutura/observabilidade da plataforma.

## Testes

```bash
dotnet test
```

Os testes de unidade (xUnit) estão em `tests/DonationWorker.Tests` e cobrem as regras de processamento do consumer e do domínio.

## Estrutura do Projeto

```
ES.ConexaoSolidaria.Worker/
├── src/
│   └── DonationWorker/
│       ├── Consumers/          # DonationCreatedEventConsumer (MassTransit)
│       ├── Domain/             # Entidades (Campanha, Doacao), Value Objects, Eventos de Domínio
│       ├── Infrastructure/     # EF Core, Repositórios, Messaging, Auditoria, Métricas
│       ├── Worker.cs           # BackgroundService
│       └── Program.cs          # Host mínimo (health check + métricas)
├── tests/
│   └── DonationWorker.Tests/   # Testes de unidade (xUnit)
├── docker-compose.yml          # Orquestração local do Worker
└── ES.ConexaoSolidaria.Worker.slnx
```

Projeto desenvolvido para o Hackathon **POSTECH** — grupo 1.

## Github Actions

O repositório contém um pipeline GitHub Actions, acionado a cada push na branch principal. O pipeline compila o código (.NET build), executa os testes e gera a imagem Docker.
