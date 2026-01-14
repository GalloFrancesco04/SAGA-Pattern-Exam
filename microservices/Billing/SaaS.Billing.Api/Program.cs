using SaaS.Billing.Api.BackgroundServices;
using Microsoft.EntityFrameworkCore;
using SaaS.Billing.Business.Services;
using SaaS.Billing.Repository.Contexts;
using SaaS.Utility.Kafka.Constants;
using SaaS.Utility.Kafka.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("BillingDb"),
        b => b.MigrationsAssembly("SaaS.Billing.Api")));
builder.Services.AddKafkaClients(options =>
    builder.Configuration.GetSection("Kafka").Bind(options));

// Business services
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Background services
builder.Services.AddHostedService<BillingProducerService>();
builder.Services.AddHostedService<BillingConsumerService>();

var app = builder.Build();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
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
