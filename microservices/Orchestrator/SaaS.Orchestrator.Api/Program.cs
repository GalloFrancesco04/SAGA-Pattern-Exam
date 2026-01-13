using Microsoft.EntityFrameworkCore;
using SaaS.Orchestrator.Business.Services;
using SaaS.Orchestrator.Repository.Contexts;

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

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
