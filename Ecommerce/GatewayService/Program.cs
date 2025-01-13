using GatewayService.Middleware;
using GatewayService.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

var productServiceadress = builder.Configuration.GetValue<string>("ProductServiceAddress");
var orderServiceadress = builder.Configuration.GetValue<string>("OrderServiceAddress");

builder.Services.AddGrpcClient<ProductServiceGRPC.ProductServiceGRPC.ProductServiceGRPCClient>(productServiceadress, options => { options.Address = new Uri(productServiceadress); });
builder.Services.AddGrpcClient<OrderServiceGRPC.OrderServiceGRPC.OrderServiceGRPCClient>(orderServiceadress, options => { options.Address = new Uri(orderServiceadress); });

builder.Services.AddControllers(options =>
{
    options.Filters.Add<CustomHeaderFilter>();
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

