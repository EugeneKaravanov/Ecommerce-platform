using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Migrations;
using OrderService.Repositories;
using OrderService.Services;
using OrderService.Utilities.Factories;
using OrderService.Validators;

var builder = WebApplication.CreateBuilder(args);
var deffaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
List<string> shardConnectionStrings = new List<string>()
{
    builder.Configuration.GetConnectionString("Shard1ConnectionString"),
    builder.Configuration.GetConnectionString("Shard2ConnectionString")
};
var productServiceadress = builder.Configuration.GetValue<string>("ProductServiceAddress");

builder.Services.AddGrpcClient<ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient>(productServiceadress, options => { options.Address = new Uri(productServiceadress); });
builder.Services.AddScoped<IOrderRepository, OrderRepository>(serviceProvider =>
{
    var productServiceClient = serviceProvider.GetRequiredService<ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient>();

    return new OrderRepository(deffaultConnectionString, productServiceClient);
});
builder.Services.AddScoped<OrderValidator>();
builder.Services.AddSingleton<ShardFactory>(serviceProvider =>
{
    return new ShardFactory(shardConnectionStrings);
});
builder.Services.AddSingleton<ShardConnectionFactory>();

builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(builder.Configuration.GetConnectionString("DefaultConnection"))
        .ScanIn(typeof(InitialMigrationForOrders).Assembly).For.Migrations());
builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<OrderGRPCService>();
using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}

app.Run();
