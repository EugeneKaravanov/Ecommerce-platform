using FluentMigrator.Runner;
using OrderService.Migrations;
using OrderService.Migrations.FirstShardDB;
using OrderService.Migrations.NoShardDB;
using OrderService.Migrations.SecondShardDB;
using OrderService.Models;
using OrderService.Repositories;
using OrderService.Services;
using OrderService.Utilities.Factories;
using OrderService.Validators;

var builder = WebApplication.CreateBuilder(args);

var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var shards = builder.Configuration.GetSection("Shards").Get<List<Shard>>();
var productServiceadress = builder.Configuration.GetValue<string>("ProductServiceAddress");

builder.Services.AddSingleton<ShardConnectionFactory>();
builder.Services.AddSingleton<ShardFactory>(serviceProvider =>
{
    return new ShardFactory(shards);
});
builder.Services.AddGrpcClient<ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient>(productServiceadress, options => { options.Address = new Uri(productServiceadress); });
builder.Services.AddScoped<IOrderRepository, ShardOrderRepository>(serviceProvider =>
{
    var productServiceClient = serviceProvider.GetRequiredService<ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient>();
    var shardConnectionFactory = serviceProvider.GetRequiredService<ShardConnectionFactory>();

    return new ShardOrderRepository(shardConnectionFactory, productServiceClient);
});
builder.Services.AddScoped<OrderValidator>();
builder.Services.AddGrpc();
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.MapGrpcService<OrderGRPCService>();

CustomMigrationRunner runner = new CustomMigrationRunner(shards);

runner.RunMigrations(defaultConnectionString, typeof(NoShardMigration).Assembly);
runner.RunMigrations(shards[0].ConnectionString, typeof(FirstShardInitialMigration).Assembly);
runner.RunMigrations(shards[1].ConnectionString, typeof(SecondShardInitialMigration).Assembly);

app.Run();
