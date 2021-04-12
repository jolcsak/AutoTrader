#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["AutoTrader.PriceCollector/AutoTrader.PriceCollector.csproj", "AutoTrader.PriceCollector/"]
COPY ["AutoTrader/AutoTrader.csproj", "AutoTrader/"]
RUN dotnet restore "AutoTrader.PriceCollector/AutoTrader.PriceCollector.csproj"
COPY . .
WORKDIR "/src/AutoTrader.PriceCollector"
RUN dotnet build "AutoTrader.PriceCollector.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AutoTrader.PriceCollector.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AutoTrader.PriceCollector.dll"]