FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
ENV ASPNETCORE_HTTP_PORTS=8080

RUN mkdir -p /var/lib/frogio/keys \
    && chown "$APP_UID" /var/lib/frogio/keys \
    && chmod 700 /var/lib/frogio/keys

USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Server/Server.csproj", "Server/"]
COPY ["Core/Core.csproj", "Core/"]
RUN dotnet restore "Server/Server.csproj"
COPY Core/ Core/
COPY Server/ Server/
WORKDIR "/src/Server"
RUN dotnet build "./Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Server.dll"]
