# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Controlume.Web/Controlume.Web.csproj", "src/Controlume.Web/"]
RUN dotnet restore "src/Controlume.Web/Controlume.Web.csproj"
COPY src/Controlume.Web/ src/Controlume.Web/
WORKDIR "/src/src/Controlume.Web"
# Sem --no-restore de propósito: com só o .csproj presente, o restore acima não
# descobre os static web assets do framework (ex.: _framework/blazor.web.js) e,
# como o publish não roda restore de novo, o arquivo nunca vai pro output publicado
# (404 em produção mesmo com o build "passando"). Rodar restore aqui de novo é
# rápido (pacotes já em cache) e resolve os assets corretamente com o código completo.
RUN dotnet publish "Controlume.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Controlume.Web.dll"]
