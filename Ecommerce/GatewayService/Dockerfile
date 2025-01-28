FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY GatewayService/GatewayService.csproj GatewayService/

RUN dotnet restore "GatewayService/GatewayService.csproj"

COPY . .

RUN dotnet publish "GatewayService/GatewayService.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "GatewayService.dll"]