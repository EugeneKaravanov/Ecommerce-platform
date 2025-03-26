FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY InformerService/InformerService.csproj InformerService/
RUN dotnet restore "InformerService/InformerService.csproj"

COPY . ./
RUN dotnet publish "InformerService/InformerService.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "InformerService.dll"]