FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY ProductService/ProductService.csproj ProductService/
RUN dotnet restore "ProductService/ProductService.csproj"

COPY . ./
RUN dotnet publish "ProductService/ProductService.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ProductService.dll"]