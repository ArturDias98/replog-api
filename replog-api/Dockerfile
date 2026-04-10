FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY replog-api.sln .
COPY replog-api/            replog-api/
COPY replog-domain/         replog-domain/
COPY replog-shared/         replog-shared/
COPY replog-application/    replog-application/
COPY replog-infrastructure/ replog-infrastructure/
RUN dotnet publish replog-api/replog-api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "replog-api.dll"]
