using ProductService.Repositories;
using ProductService.Validators;
using ProductService.Services;
using System.Data;
using Npgsql;
using FluentMigrator.Runner;
using ProductService.Migrations;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

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

var app = builder.Build();

app.MapGrpcService<ProductGRPCService>();
using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}

app.Run();