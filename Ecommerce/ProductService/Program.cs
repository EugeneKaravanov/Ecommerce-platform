using ProductService.Repositories;
using ProductService.Validators;
using ProductService.Services;
using System.Data;
using Npgsql;
using FluentMigrator.Runner;
using ProductService.Migrations;
using ProductService.Models.Kafka;
using ProductService.Models.Kafka.KafkaMessages;
using ProductService.Services.Consumers;
using MassTransit;
using MassTransit.KafkaIntegration;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var kafka = builder.Configuration.GetSection("Kafka").Get<KafkaInfo>();

builder.Services.AddSingleton<IProductRepository, ProductRepository>(_ => new (connectionString));
builder.Services.AddSingleton<ProductValidator>();
builder.Services.AddScoped<IDbConnection>(_ =>
{
    return new NpgsqlConnection(connectionString);
});

builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(builder.Configuration.GetConnectionString("DefaultConnection"))
        .ScanIn(typeof(InitialMigration).Assembly).For.Migrations());
builder.Services.AddGrpc();
builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory();
    x.AddRider(rider =>
    {
        rider.AddProducer<ProductsReserved>(kafka.ProductsReservedTopic);
        rider.AddConsumer<OrderCreatedKafkaConsumer>();

        rider.UsingKafka((context, k) =>
        {
            k.Host(kafka.Adress);

            k.TopicEndpoint<string, OrderCreated>(kafka.OrderCreatedTopic, "ProductServicesConsumerGroup", e =>
            {
                e.ConfigureConsumer<OrderCreatedKafkaConsumer>(context);
            });
        });
    });
});

var app = builder.Build();

app.MapGrpcService<ProductGRPCService>();
using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}

app.Run();