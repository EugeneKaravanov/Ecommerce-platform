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
var port = builder.Configuration.GetValue<int>("PORT", 8080);
var url = $"http://0.0.0.0:{port}";

builder.WebHost.UseUrls(url);

var deffaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
List<string> shardConnectionStrings = new List<string>()
{
    builder.Configuration.GetConnectionString("FirstShardConnectionString"),
    builder.Configuration.GetConnectionString("SecondShardConnectionString")
};
var productServiceadress = builder.Configuration.GetValue<string>("ProductServiceAddress");

builder.Services.AddSingleton<ShardConnectionFactory>();
builder.Services.AddSingleton<ShardFactory>(serviceProvider =>
{
    return new ShardFactory(shardConnectionStrings);
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

CustomMigrationRunner runner = new CustomMigrationRunner(shardConnectionStrings);

runner.RunMigrations(deffaultConnectionString, typeof(NoShardMigration).Assembly);
runner.RunMigrations(shardConnectionStrings[0], typeof(FirstShardInitialMigration).Assembly);
runner.RunMigrations(shardConnectionStrings[1], typeof(SecondShardInitialMigration).Assembly);

app.Run();
