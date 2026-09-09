using DocuMind.Api.Extensions;
using DocuMind.Api.Middleware;
using DocuMind.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddTokenAuthentication(builder.Configuration);
builder.Services.AddSpaCors(builder.Configuration);

// Restricting to JSON keeps the OpenAPI document (and the generated client) free of
// text/plain and text/json duplicates of every response type.
builder.Services.AddControllers(options =>
    options.Filters.Add(new ProducesAttribute("application/json")));

var app = builder.Build();

// First in the pipeline so it can catch anything thrown further down.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Serves the generated spec at /openapi/v1.json ...
    app.MapOpenApi();

    // ... and points Swagger UI at it. Only the UI package is referenced: AddOpenApi above
    // already builds the document, so Swashbuckle's own generator would be a second, competing pipeline.
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "DocuMind API v1"));
}
else
{
    // Redirecting in development would break the SPA's plain-HTTP calls to port 5100.
    app.UseHttpsRedirection();
}

app.UseCors(ServiceCollectionExtensions.SpaCorsPolicy);

// Order matters: authentication populates the principal, authorization then inspects it.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", (IHealthService health) => health.GetStatus())
    .WithName("GetHealth");

app.Run();
