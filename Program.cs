using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Simapd.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
builder.Services.AddDbContext<SimapdDb>(opt
    => opt.UseNpgsql(dbConnectionString));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", Ok<HealthCheck> () => {
    return TypedResults.Ok(new HealthCheck("Healthy", DateTime.UtcNow));
})
.WithName("HealthCheck")
.WithDisplayName("Application Health Check")
.WithDescription("""
    Verifica o status de saúde da aplicação e retorna informações básicas de funcionamento.

    CAMPOS DE RETORNO:
    • Status (string): Status atual da aplicação ("Healthy")
    • Timestamp (DateTime): Data/hora UTC exata quando a verificação foi realizada

    POSSÍVEIS FALHAS:
    • 500 Internal Server Error: Falha crítica na aplicação (Inacessivel)

    USO RECOMENDADO:
    Este endpoint deve ser chamado por load balancers, ferramentas de monitoramento (Kubernetes liveness/readiness probes)
    e sistemas de alertas para verificar se a aplicação está funcionando adequadamente.
    """)
.WithSummary("Endpoint para verificação de saúde da aplicação")
.WithTags("Health", "Monitoring", "System")
.ProducesProblem(StatusCodes.Status500InternalServerError)
.AllowAnonymous()
.CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30)).Tag("health-check"));

app.Run();
