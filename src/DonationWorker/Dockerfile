# --------------------------------------------------------------------------------------------------
# STAGE: BASE (Execução/Runtime)
# Onde a aplicação irá rodar.
# --------------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Cria um grupo e um usuário não-root (FIAP) com um ID específico (ex: 1000)
# Usar um UID/GID fixo ajuda na gestão de permissões de volumes.
ARG APP_USER=FIAP
ARG APP_UID=1000
RUN groupadd -r $APP_USER -g $APP_UID && \
    useradd -r -g $APP_USER -u $APP_UID -s /sbin/nologin -c "Docker User" $APP_USER

WORKDIR /

# Define o usuário não-root para execução.
USER $APP_USER 



# --------------------------------------------------------------------------------------------------
# STAGE: BUILD
# Usa o SDK para compilar. 
# --------------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/DonationWorker/DonationWorker.csproj", "src/DonationWorker/"]
RUN dotnet restore "./src/DonationWorker/DonationWorker.csproj"
COPY . .
WORKDIR "/src/src/DonationWorker"
RUN dotnet build "./DonationWorker.csproj" -c $BUILD_CONFIGURATION -o /app/build



# --------------------------------------------------------------------------------------------------
# STAGE: PUBLISH
# --------------------------------------------------------------------------------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DonationWorker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false



# --------------------------------------------------------------------------------------------------
# STAGE: FINAL (Produção)
# Copia os binários e garante que o usuário FIAP tem as permissões corretas
# --------------------------------------------------------------------------------------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DonationWorker.dll"]