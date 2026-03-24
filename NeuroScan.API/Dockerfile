FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["NeuroScan.API/NeuroScan.API.csproj", "NeuroScan.API/"]
COPY ["NeuroScan.Application/NeuroScan.Application.csproj", "NeuroScan.Application/"]
COPY ["NeuroScan.Domain/NeuroScan.Domain.csproj", "NeuroScan.Domain/"]
COPY ["NeuroScan.Infrastructure/NeuroScan.Infrastructure.csproj", "NeuroScan.Infrastructure/"]
COPY ["NeuroScan.Shared/NeuroScan.Shared.csproj", "NeuroScan.Shared/"]

RUN dotnet restore "NeuroScan.API/NeuroScan.API.csproj"
COPY . .
WORKDIR "/src/NeuroScan.API"
RUN dotnet build "NeuroScan.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NeuroScan.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NeuroScan.API.dll"]
