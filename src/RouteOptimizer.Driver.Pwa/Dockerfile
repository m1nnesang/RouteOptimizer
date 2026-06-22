FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app
COPY src/RouteOptimizer.Driver.Pwa/RouteOptimizer.Driver.Pwa.csproj src/RouteOptimizer.Driver.Pwa/
COPY Directory.Packages.props .
COPY Directory.Build.props .

RUN dotnet restore src/RouteOptimizer.Driver.Pwa/RouteOptimizer.Driver.Pwa.csproj
COPY . .
RUN dotnet publish src/RouteOptimizer.Driver.Pwa/RouteOptimizer.Driver.Pwa.csproj -c Release -o /app/publish

FROM nginx:alpine AS runtime

COPY src/RouteOptimizer.Driver.Pwa/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 80
