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

Campos de retorno:
- Status (string): Status atual da aplicação ("Healthy")
- Timestamp (DateTime): Data/hora UTC exata quando a verificação foi realizada

Possíveis falhas:
- 500 Internal Server Error: Falha crítica na aplicação (Inacessivel)

Uso recomendado:
Este endpoint deve ser chamado por load balancers, ferramentas de monitoramento (Kubernetes liveness/readiness probes)
e sistemas de alertas para verificar se a aplicação está funcionando adequadamente.
""")
.WithSummary("Endpoint para verificação de saúde da aplicação")
.WithTags("Health", "Monitoring", "System")
.ProducesProblem(StatusCodes.Status500InternalServerError)
.AllowAnonymous()
.CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30)).Tag("health-check"));

var riskAreaGroup = app.MapGroup("/risk-areas").WithTags("Risk Areas").WithDescription("Endpoint related to Risk Areas control");

riskAreaGroup.MapGet("/", async Task<Results<Ok<PagedResponseDto<RiskAreaDto>>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IMapper mapper,
    int pageNumber = 1,
    int pageSize = 10
) => {
    if (pageNumber <= 0) {
        return TypedResults.BadRequest(new ErrorResponse(400, "O número da página deve ser maior que zero."));
    }

    if (pageSize <= 0) {
        return TypedResults.BadRequest(new ErrorResponse(400, "O tamanho da página deve ser maior que zero."));
    }

    var riskAreas = await riskAreaRepository.ListPagedAsync(pageNumber, pageSize);

    return TypedResults.Ok(mapper.Map<PagedResponseDto<RiskAreaDto>>(riskAreas));
})
.WithSummary("Lista paginada das áreas de risco registradas.")
.WithDescription("""
Retorna uma lista paginada das áreas de risco disponíveis no sistema.

Parâmetros:
- pageNumber (opcional): Número da página a ser retornada. Deve ser maior que zero. Padrão: 1
- pageSize (opcional): Quantidade de itens por página. Deve ser maior que zero. Padrão: 10

Respostas:
- 200 OK: Retorna uma estrutura paginada (`PagedResponseDto<RiskAreaDto>`) contendo os dados das áreas de risco.
- 400 Bad Request: Retornado quando `pageNumber` ou `pageSize` são menores ou iguais a zero.
""");

riskAreaGroup.MapPost("/", async Task<Results<Ok<RiskAreaDto>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IMapper mapper,
    RiskAreaRequestDto newRiskArea
) => {
    var riskArea = await riskAreaRepository.CreateAsync(mapper.Map<RiskArea>(newRiskArea));

    return TypedResults.Ok(mapper.Map<RiskAreaDto>(riskArea));
})
.WithSummary("Registra uma nova área de risco.")
.WithDescription("""
Registra uma nova área de risco no sistema.
""");

app.Run();
