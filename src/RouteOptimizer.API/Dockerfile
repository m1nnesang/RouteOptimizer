FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app
COPY src/RouteOptimizer.API/RouteOptimizer.API.csproj src/RouteOptimizer.API/
COPY src/RouteOptimizer.Application/RouteOptimizer.Application.csproj src/RouteOptimizer.Application/
COPY src/RouteOptimizer.Domain/RouteOptimizer.Domain.csproj src/RouteOptimizer.Domain/
COPY src/RouteOptimizer.Infrastructure/RouteOptimizer.Infrastructure.csproj src/RouteOptimizer.Infrastructure/
COPY Directory.Packages.props .

RUN dotnet restore src/RouteOptimizer.API/RouteOptimizer.API.csproj
COPY . .
RUN dotnet publish src/RouteOptimizer.API/RouteOptimizer.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "RouteOptimizer.API.dll"]
