using Microsoft.EntityFrameworkCore;
using SaaS.Orchestrator.Api.Services;
using SaaS.Orchestrator.Business.Services;
using SaaS.Orchestrator.ClientHttp.Clients;
using SaaS.Orchestrator.Repository.Contexts;
using SaaS.Utility.Kafka.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<OrchestratorDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("OrchestratorDb"),
        b => b.MigrationsAssembly("SaaS.Orchestrator.Api")
    ));
builder.Services.AddScoped<ISagaService, SagaService>();
builder.Services.AddKafkaClients(options => builder.Configuration.GetSection("Kafka").Bind(options));
builder.Services.AddHostedService<OrchestratorProducerService>();
builder.Services.AddHostedService<OrchestratorConsumerService>();

// Configure HTTP Clients for synchronous communication
builder.Services.AddHttpClient<IBillingClient, BillingClient>(client =>
{
    var baseUrl = builder.Configuration["Services:Billing:Url"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IProvisioningClient, ProvisioningClient>(client =>
{
    var baseUrl = builder.Configuration["Services:Provisioning:Url"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
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
