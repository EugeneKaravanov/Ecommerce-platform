using System.Data;
using FluentMigrator.Runner;
using Npgsql;
using OrderService.Migrations;
using OrderService.Repositories;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var productServiceadress = builder.Configuration.GetValue<string>("ProductServiceAddress");

builder.Services.AddGrpcClient<ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient>(productServiceadress, options => { options.Address = new Uri(productServiceadress); });
builder.Services.AddScoped<IOrderRepository, OrderRepository>(serviceProvider =>
{
    var tempConectionString = connectionString;
    var productServiceClient = serviceProvider.GetRequiredService<ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient>();

    return new OrderRepository(connectionString, productServiceClient);
});
builder.Services.AddScoped<IDbConnection>(_ =>
{
    return new NpgsqlConnection(connectionString);
});

builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(builder.Configuration.GetConnectionString("DefaultConnection"))
        .ScanIn(typeof(InitialMigrationForOrders).Assembly).For.Migrations());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}

app.Run();
