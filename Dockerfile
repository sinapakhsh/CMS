FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore CMS.slnx --locked-mode
RUN dotnet publish src/CMS.Api/CMS.Api.csproj -c Release --no-restore -o /output/api
RUN dotnet publish src/CMS.Migrator/CMS.Migrator.csproj -c Release --no-restore -o /output/migrator

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrator
WORKDIR /app
COPY --from=build /output/migrator .
USER $APP_UID
ENTRYPOINT ["dotnet", "CMS.Migrator.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /output/api .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CMS.Api.dll"]
