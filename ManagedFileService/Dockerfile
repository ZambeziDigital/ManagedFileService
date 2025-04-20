FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
#RUN chown app:app /app

EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ManagedFileService/ManagedFileService.csproj", "ManagedFileService/"]
RUN dotnet restore "ManagedFileService/ManagedFileService.csproj"
COPY . .
WORKDIR "/src/ManagedFileService"
RUN dotnet build "ManagedFileService.csproj" --no-cache -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ManagedFileService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER root
RUN mkdir -p /app/uploads && chmod -R 777 /app/uploads
ENTRYPOINT ["dotnet", "ManagedFileService.dll"]
