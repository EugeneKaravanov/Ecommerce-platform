using FluentMigrator.Runner;
using OrderService.Migrations;
using OrderService.Migrations.FirstShardDB;
using OrderService.Migrations.NoShardDB;
using OrderService.Migrations.SecondShardDB;
using OrderService.Repositories;
using OrderService.Services;
using OrderService.Utilities.Factories;
using OrderService.Validators;

var builder = WebApplication.CreateBuilder(args);
var deffaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
List<string> shardConnectionStrings = new List<string>()
{
    builder.Configuration.GetConnectionString("FirstShardConnectionString"),
    builder.Configuration.GetConnectionString("SecondShardConnectionString")
};
var productServiceadress = builder.Configuration.GetValue<string>("ProductServiceAddress");

builder.Services.AddGrpcClient<ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient>(productServiceadress, options => { options.Address = new Uri(productServiceadress); });
builder.Services.AddScoped<IOrderRepository, OrderRepository>(serviceProvider =>
{
    var productServiceClient = serviceProvider.GetRequiredService<ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient>();

    return new OrderRepository(deffaultConnectionString, productServiceClient);
});
builder.Services.AddScoped<OrderValidator>();
builder.Services.AddSingleton<ShardConnectionFactory>();
builder.Services.AddSingleton<ShardFactory>(serviceProvider =>
{
    return new ShardFactory(shardConnectionStrings);
});
builder.Services.AddGrpc();
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.MapGrpcService<OrderGRPCService>();

CustomMigrationRunner runner = new CustomMigrationRunner(shardConnectionStrings);

runner.RunMigrationsForNoShardDB(deffaultConnectionString);
runner.RunMigrationsForFirstShardDB(shardConnectionStrings[0]);
runner.RunMigrationForSecondShardDB(shardConnectionStrings[1]);

app.Run();
