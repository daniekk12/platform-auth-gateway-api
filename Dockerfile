# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Platform.Auth.Gateway.Api.csproj", "./"]
RUN dotnet restore "Platform.Auth.Gateway.Api.csproj"

COPY . .
RUN dotnet publish "Platform.Auth.Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN groupadd --system appgroup \
    && useradd --system --gid appgroup --home /app appuser \
    && chown -R appuser:appgroup /app

USER appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Platform.Auth.Gateway.Api.dll"]
