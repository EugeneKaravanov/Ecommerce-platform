FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY OrderService/OrderService.csproj OrderService/
COPY OrderService.FirstShardDB.Migrations/OrderService.FirstShardDB.Migrations.csproj OrderService.FirstShardDB.Migrations/
COPY OrderService.NoShardDB.Migrations/OrderService.NoShardDB.Migrations.csproj OrderService.NoShardDB.Migrations/
COPY OrderService.SecondShardDB.Migrations/OrderService.SecondShardDB.Migrations.csproj OrderService.SecondShardDB.Migrations/
COPY OrderService.Common/OrderService.Common.csproj OrderService.Common/

RUN dotnet restore "OrderService/OrderService.csproj"

COPY OrderService/. OrderService/
COPY OrderService.FirstShardDB.Migrations/. OrderService.FirstShardDB.Migrations/
COPY OrderService.NoShardDB.Migrations/. OrderService.NoShardDB.Migrations/
COPY OrderService.SecondShardDB.Migrations/. OrderService.SecondShardDB.Migrations/
COPY OrderService.Common/. OrderService.Common/

RUN dotnet publish "OrderService/OrderService.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderService.dll"]