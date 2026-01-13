using Microsoft.EntityFrameworkCore;
using SaaS.Notification.Api.Services;
using SaaS.Notification.Business.Services;
using SaaS.Notification.Repository.Contexts;
using SaaS.Utility.Kafka.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Database
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NotificationDb"),
        b => b.MigrationsAssembly("SaaS.Notification.Api")));

// Business services
builder.Services.AddScoped<IEmailService, EmailService>();

// Kafka
builder.Services.AddKafkaClients(options =>
{
    builder.Configuration.GetSection("Kafka").Bind(options);
});

// Background services
builder.Services.AddHostedService<NotificationProducerService>();
builder.Services.AddHostedService<NotificationConsumerService>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    try
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
    catch (Exception ex)
    {
        // Log but don't fail startup
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Failed to ensure database creation");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
