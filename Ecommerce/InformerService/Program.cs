using InformerService.Models.Kafka;
using InformerService.Models.Kafka.KafkaMessages;
using InformerService.Services;
using InformerService.Services.Consumers;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);
var kafka = builder.Configuration.GetSection("Kafka").Get<KafkaInfo>();

builder.Services.AddScoped<UserInformer>();
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});
builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory();
    x.AddRider(rider =>
    {
        rider.AddConsumer<OrderFormedKafkaConsumer>();

        rider.UsingKafka((context, k) =>
        {
            k.Host(kafka.Adress);

            k.TopicEndpoint<string, OrderFormed>(kafka.OrderFormedTopic, "InformerServicesConsumerGroup", e =>
            {
                e.ConfigureConsumer<OrderFormedKafkaConsumer>(context);
            });
        });
    });
});

var app = builder.Build();

app.Run();
