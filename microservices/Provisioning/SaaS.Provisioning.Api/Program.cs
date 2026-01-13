using Microsoft.EntityFrameworkCore;
using SaaS.Provisioning.Repository.Contexts;
using SaaS.Provisioning.Business.Services;

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
