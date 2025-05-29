using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Simapd.Models;
using Simapd.Dtos;
using Simapd.Repositories;
using AutoMapper;
using Simapd.Profiles;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
builder.Services.AddDbContext<SimapdDb>(opt
    => opt.UseNpgsql(dbConnectionString));

builder.Services.AddAutoMapper(typeof(RiskAreaProfile));

builder.Services.AddScoped<IRiskAreaRepository, RiskAreaRepository>();

var app = builder.Build();


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

var riskAreaGroup = app.MapGroup("/risk-areas").WithTags("Risk Areas").WithDescription("Endpoint related to Risk Areas control");

riskAreaGroup.MapGet("/", async Task<Results<Ok<PagedResponseDto<RiskAreaDto>>, BadRequest>> (
    IRiskAreaRepository riskAreaRepository,
    IMapper mapper,
    int pageNumber = 1,
    int pageSize = 10
) => {
    if (pageNumber <= 0) {
        return TypedResults.BadRequest(
            // TypedResults.Problem(
            //     title: "Bad Request",
            //     detail: $"{nameof(pageNumber)} must be greater than 0",
            //     statusCode: StatusCodes.Status400BadRequest
            // )
        );
    }

    if (pageSize <= 0) {
        return TypedResults.BadRequest(
            // TypedResults.Problem(
            //     title: "Bad Request",
            //     detail: $"{nameof(pageSize)} must be greater than 0",
            //     statusCode: StatusCodes.Status400BadRequest
            // )
        );
    }

    var riskAreas = await riskAreaRepository.ListPagedAsync(pageNumber, pageSize);

    return TypedResults.Ok(mapper.Map<PagedResponseDto<RiskAreaDto>>(riskAreas));
})
.WithSummary("Returns a paginated result of all risk areas");

app.Run();
