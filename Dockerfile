FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["CryptoExchangeWebApi/CryptoExchangeWebApi.csproj", "CryptoExchangeWebApi/"]
RUN dotnet restore "CryptoExchangeWebApi/CryptoExchangeWebApi.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "CryptoExchangeWebApi/CryptoExchangeWebApi.csproj" -c Release -o CryptoExchangeWebApi/app/build

FROM build AS publish
RUN dotnet publish "CryptoExchangeWebApi/CryptoExchangeWebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CryptoExchangeWebApi.dll"]
