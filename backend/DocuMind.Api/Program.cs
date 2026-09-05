using DocuMind.Application.Interfaces;
using DocuMind.Application.Services;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<IHealthService, HealthService>();

// Document feature: the service holds the use-case logic, the repository the EF Core access.
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

builder.Services.AddDbContext<DocuMindDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Registers the MVC machinery that discovers and invokes controller classes.
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Serves the generated spec at /openapi/v1.json ...
    app.MapOpenApi();

    // ... and points Swagger UI at it. Only the UI package is referenced: AddOpenApi above
    // already builds the document, so Swashbuckle's own generator would be a second, competing pipeline.
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "DocuMind API v1"));
}

app.UseHttpsRedirection();

// Routes matching requests into those controllers. Without this, AddControllers above is inert.
app.MapControllers();

app.MapGet("/health", (IHealthService health) => health.GetStatus())
    .WithName("GetHealth");

app.Run();
