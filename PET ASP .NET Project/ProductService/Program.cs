using ProductService.Repositories;
using ProductService.Validators;
using ProductService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddSingleton<ProductValidator>();
builder.Services.AddSingleton<ProductValidatorService>();
builder.Services.AddGrpc();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.MapGrpcService<ProductGRPCService>();
app.Run();