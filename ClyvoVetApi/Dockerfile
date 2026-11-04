# =============================================
# STAGE 1: Build
# O contexto de build é a RAIZ da solução (pasta que contem ClyvoVetApi.sln),
# pois a API depende dos projetos Domain, Application e Infrastructure.
#   docker build -f ClyvoVetApi/Dockerfile -t clyvovet-api .
# =============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia apenas os .csproj e restaura (cache de camadas)
COPY ["ClyvoVetApi/ClyvoVetApi.csproj", "ClyvoVetApi/"]
COPY ["ClyvoVetApi.Application/ClyvoVetApi.Application.csproj", "ClyvoVetApi.Application/"]
COPY ["ClyvoVetApi.Domain/ClyvoVetApi.Domain.csproj", "ClyvoVetApi.Domain/"]
COPY ["ClyvoVetApi.Infrastructure/ClyvoVetApi.Infrastructure.csproj", "ClyvoVetApi.Infrastructure/"]
RUN dotnet restore "ClyvoVetApi/ClyvoVetApi.csproj"

# Copia o codigo-fonte das camadas e publica em Release
COPY ClyvoVetApi/ ClyvoVetApi/
COPY ClyvoVetApi.Application/ ClyvoVetApi.Application/
COPY ClyvoVetApi.Domain/ ClyvoVetApi.Domain/
COPY ClyvoVetApi.Infrastructure/ ClyvoVetApi.Infrastructure/
RUN dotnet publish "ClyvoVetApi/ClyvoVetApi.csproj" -c Release -o /app/publish --no-restore

# =============================================
# STAGE 2: Runtime
# =============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "ClyvoVetApi.dll"]