using GatewayService.Middleware;
using GatewayService.Filters;

var builder = WebApplication.CreateBuilder(args);

var productServiceAddress = builder.Configuration.GetValue<string>("ProductServiceAddress");
var orderServiceAddress = builder.Configuration.GetValue<string>("OrderServiceAddress");

builder.Services.AddGrpcClient<ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient>(productServiceAddress, options => { options.Address = new Uri(productServiceAddress); });
builder.Services.AddGrpcClient<OrderServiceGRPC.OrderServiceGRPC.OrderServiceGRPCClient>(orderServiceAddress, options => { options.Address = new Uri(orderServiceAddress); });

builder.Services.AddControllers(options =>
{
    options.Filters.Add<CustomHeaderFilter>();
});
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.MapControllers();
app.Run();

