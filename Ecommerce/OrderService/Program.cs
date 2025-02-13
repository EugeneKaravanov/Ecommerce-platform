using Confluent.Kafka;
using FluentMigrator.Runner;
using MassTransit;
using MassTransit.KafkaIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OrderService.Migrations;
using OrderService.Migrations.FirstShardDB;
using OrderService.Migrations.NoShardDB;
using OrderService.Migrations.SecondShardDB;
using OrderService.Models;
using OrderService.Models.Kafka;
using OrderService.Models.Kafka.KafkaMessages;
using OrderService.Repositories;
using OrderService.Services;
using OrderService.Services.Consumers;
using OrderService.Utilities.Factories;
using OrderService.Validators;

var builder = WebApplication.CreateBuilder(args);

var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var shards = builder.Configuration.GetSection("Shards").Get<List<Shard>>();
var productServiceadress = builder.Configuration.GetValue<string>("ProductServiceAddress");
var kafka = builder.Configuration.GetSection("Kafka").Get<KafkaInfo>();

builder.Services.AddSingleton<ShardConnectionFactory>();
builder.Services.AddSingleton<ShardFactory>(serviceProvider =>
{
    return new ShardFactory(shards);
});
builder.Services.AddGrpcClient<ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient>(productServiceadress, options => { options.Address = new Uri(productServiceadress); });
builder.Services.AddScoped<OrderValidator>();
builder.Services.AddGrpc();
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory();
    x.AddRider(rider =>
    {
        rider.AddProducer<OrderCreated>(kafka.OrderCreatedTopic);
        rider.AddConsumer<ProductsReservedKafkaConsumer>();

        rider.UsingKafka((context, k) =>
        {
            k.Host(kafka.Adress);

            k.TopicEndpoint<string, ProductsReserved>(kafka.ProductsReservedTopic, "ProductsReservedConsumerGroup", e =>
            {
                e.ConfigureConsumer<ProductsReservedKafkaConsumer>(context);
            });
        });
    });
});

builder.Services.AddScoped<IOrderRepository, ShardOrderRepository>(serviceProvider =>
{
    var productServiceClient = serviceProvider.GetRequiredService<ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient>();
    var shardConnectionFactory = serviceProvider.GetRequiredService<ShardConnectionFactory>();
    var producer = serviceProvider.GetRequiredService<ITopicProducer<OrderCreated>>();

    return new ShardOrderRepository(shardConnectionFactory, productServiceClient, producer);
});

builder.Services.AddLogging();

var app = builder.Build();

app.MapGrpcService<OrderGRPCService>();

CustomMigrationRunner runner = new CustomMigrationRunner(shards);

runner.RunMigrations(defaultConnectionString, typeof(NoShardMigration).Assembly);
runner.RunMigrations(shards[0].ConnectionString, typeof(FirstShardInitialMigration).Assembly);
runner.RunMigrations(shards[1].ConnectionString, typeof(SecondShardInitialMigration).Assembly);

app.Run();
