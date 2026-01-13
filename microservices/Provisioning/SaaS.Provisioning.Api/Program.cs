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
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProvisioningDb")));

// Register business services
builder.Services.AddScoped<ITenantService, TenantService>();

// Register Kafka clients
builder.Services.AddKafkaClients(options =>
    builder.Configuration.GetSection("Kafka").Bind(options));

// Register background services
builder.Services.AddHostedService<ProvisioningProducerService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
