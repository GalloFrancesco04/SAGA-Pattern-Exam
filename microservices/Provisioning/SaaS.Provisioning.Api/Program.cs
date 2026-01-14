using Microsoft.EntityFrameworkCore;
using SaaS.Provisioning.Repository.Contexts;
using SaaS.Provisioning.Business.Services;
using SaaS.Provisioning.Api.Services;
using SaaS.Utility.Kafka.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddDbContext<ProvisioningDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ProvisioningDb"),
        b => b.MigrationsAssembly("SaaS.Provisioning.Api")));

// Register business services
builder.Services.AddScoped<ITenantService, TenantService>();

// Register Kafka clients
builder.Services.AddKafkaClients(options =>
    builder.Configuration.GetSection("Kafka").Bind(options));

// Register background services
builder.Services.AddHostedService<ProvisioningProducerService>();
builder.Services.AddHostedService<ProvisioningConsumerService>();

var app = builder.Build();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProvisioningDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        // Log but don't fail startup
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Failed to apply database migrations");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
