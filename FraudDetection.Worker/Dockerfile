FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app 
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FraudDetection.Worker/FraudDetection.Worker.csproj", "FraudDetection.Worker/"]
COPY ["FraudDetection.Shared/FraudDetection.Shared.csproj", "FraudDetection.Shared/"]
RUN dotnet restore "FraudDetection.Worker/FraudDetection.Worker.csproj"
COPY . .
WORKDIR "/src/FraudDetection.Worker"
RUN dotnet build "FraudDetection.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "FraudDetection.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app    
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FraudDetection.Worker.dll"]