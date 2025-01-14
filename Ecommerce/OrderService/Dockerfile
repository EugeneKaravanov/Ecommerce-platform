FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY OrderService/OrderService.csproj OrderService/

COPY OrderService.Common/OrderService.Common.csproj OrderService.Common/
COPY OrderService.FirstShardDb.Migrations/OrderService.FirstShardDb.Migrations.csproj OrderService.FirstShardDb.Migrations/
COPY OrderService.NoShardDb.Migrations/OrderService.NoShardDb.Migrations.csproj OrderService.NoShardDb.Migrations/
COPY OrderService.SecondShardDb.Migrations/OrderService.SecondShardDb.Migrations.csproj OrderService.SecondShardDb.Migrations/

COPY OrderService.Tests/OrderService.Tests.csproj OrderService.Tests/
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Debug -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderService.dll"]