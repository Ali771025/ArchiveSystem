FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src ./src

RUN dotnet restore ./src/ArchiveSystem.Worker.Thumbnail/ArchiveSystem.Worker.Thumbnail.csproj
RUN dotnet publish ./src/ArchiveSystem.Worker.Thumbnail/ArchiveSystem.Worker.Thumbnail.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV DOTNET_ENVIRONMENT=Production

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "ArchiveSystem.Worker.Thumbnail.dll"]
