using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Simapd.Models;
using Simapd.Dtos;
using Simapd.Repositories;
using AutoMapper;
using Simapd.Profiles;
using System.Text.RegularExpressions;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
builder.Services.AddDbContext<SimapdDb>(opt
    => opt.UseNpgsql(dbConnectionString));

builder.Services.AddAutoMapper(typeof(RiskAreaProfile));
builder.Services.AddAutoMapper(typeof(SensorProfile));

builder.Services.AddScoped<IRiskAreaRepository, RiskAreaRepository>();
builder.Services.AddScoped<ISensorRepository, SensorRepository>();

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

riskAreaGroup.MapGet("/{id}", async Task<Results<Ok<RiskAreaDto>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IMapper mapper,
    string id
) => {
    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "O id nao segue um formato de CUID2 valido."));
    }

    var riskArea = await riskAreaRepository.FindAsync(id);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Área de risco com id {id} nao encontrada"));
    }

    return TypedResults.Ok(mapper.Map<RiskAreaDto>(riskArea));
})
.WithSummary("Retorna a área de risco especificada.")
.WithDescription("""
Retorna a área de risco com o id enviado.
""");

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

riskAreaGroup.MapPost("/", async Task<Results<Created<RiskAreaDto>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IMapper mapper,
    RiskAreaRequestDto newRiskArea
) => {
    var riskArea = await riskAreaRepository.CreateAsync(mapper.Map<RiskArea>(newRiskArea));

    return TypedResults.Created($"/risk-area/${riskArea.Id}",mapper.Map<RiskAreaDto>(riskArea));
})
.WithSummary("Registra uma nova área de risco.")
.WithDescription("""
Registra uma nova área de risco no sistema.
""");

riskAreaGroup.MapPut("/{id}", async Task<Results<Ok<RiskAreaDto>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IMapper mapper,
    string id,
    RiskAreaRequestDto updatedRiskArea
) => {
    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "O id nao segue um formato de CUID2 valido."));
    }

    var riskArea = await riskAreaRepository.FindAsync(id);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Área de risco com id {id} nao encontrada"));
    }

    mapper.Map(updatedRiskArea, riskArea);
    await riskAreaRepository.UpdateAsync();

    return TypedResults.Ok(mapper.Map<RiskAreaDto>(riskArea));
})
.WithSummary("Atualiza a área de risco especificada.")
.WithDescription("""
Atualiza a área de risco com o id enviado.
""");

riskAreaGroup.MapDelete("/{id}", async Task<Results<NoContent, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IMapper mapper,
    string id
) => {
    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "O id nao segue um formato de CUID2 valido."));
    }

    var riskArea = await riskAreaRepository.FindAsync(id);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Área de risco com id {id} nao encontrada"));
    }

    await riskAreaRepository.DeleteAsync(riskArea);

    return TypedResults.NoContent();
})
.WithSummary("Deleta a área de risco especificada.")
.WithDescription("""
Deleta a área de risco com o id enviado.
""");

var sensorsGroup = app.MapGroup("/risk-areas/{areaId}/sensors").WithTags("Sensors").WithDescription("Endpoint related to Sensors control");

sensorsGroup.MapGet("/", async Task<Results<Ok<PagedResponseDto<SensorDto>>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    ISensorRepository sensorRepository,
    IMapper mapper,
    string areaId,
    int pageNumber = 1,
    int pageSize = 10
) => {
    if (pageNumber <= 0) {
        return TypedResults.BadRequest(new ErrorResponse(400, "O número da página deve ser maior que zero."));
    }

    if (pageSize <= 0) {
        return TypedResults.BadRequest(new ErrorResponse(400, "O tamanho da página deve ser maior que zero."));
    }

    var area = await riskAreaRepository.FindAsync(areaId);

    if (area is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Área de risco com id {areaId} nao encontrada"));
    }

    var sensors = await sensorRepository.ListPagedAsync(areaId, pageNumber, pageSize);

    return TypedResults.Ok(mapper.Map<PagedResponseDto<SensorDto>>(sensors));
})
.WithSummary("Lista paginada dos sensores registrados na área de risco especificada.")
.WithDescription("""
Retorna uma lista paginada dos sensores de uma área de risco disponíveis no sistema.

Parâmetros:
- pageNumber (opcional): Número da página a ser retornada. Deve ser maior que zero. Padrão: 1
- pageSize (opcional): Quantidade de itens por página. Deve ser maior que zero. Padrão: 10

Respostas:
- 200 OK: Retorna uma estrutura paginada (`PagedResponseDto<RiskAreaDto>`) contendo os dados das áreas de risco.
- 400 Bad Request: Retornado quando `pageNumber` ou `pageSize` são menores ou iguais a zero.
- 404 Not Found: Retornado quando o id de área passado não corresponde a uma área cadastrada no sistema.
""");

sensorsGroup.MapGet("/{id}", async Task<Results<Ok<SensorDto>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    ISensorRepository sensorRepository,
    IMapper mapper,
    string areaId,
    string id
) => {
    if (string.IsNullOrWhiteSpace(areaId) || !Regex.IsMatch(areaId, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "O areaId nao segue um formato de CUID2 valido."));
    }

    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "O id nao segue um formato de CUID2 valido."));
    }

    var riskArea = await riskAreaRepository.FindAsync(areaId);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Área de risco com id {areaId} nao encontrada"));
    }

    var sensor = await sensorRepository.FindAsync(id);

    return TypedResults.Ok(mapper.Map<SensorDto>(sensor));
})
.WithSummary("Retorna o sensor especificado.")
.WithDescription("""
Retorna o sensor de uma área de risco com os ids especificados.
""");

sensorsGroup.MapPost("/", async Task<Results<Created<SensorDto>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    ISensorRepository sensorRepository,
    IMapper mapper,
    string areaId,
    SensorRequestDto newSensor
) => {
    if (string.IsNullOrWhiteSpace(areaId) || !Regex.IsMatch(areaId, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "O areaId nao segue um formato de CUID2 valido."));
    }

    var riskArea = await riskAreaRepository.FindAsync(areaId);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Área de risco com id {areaId} nao encontrada"));
    }

    var newSensorParse = mapper.Map<Sensor>(newSensor);
    newSensorParse.AreaId = areaId;

    if (newSensor.InstalledAt is null) {
        newSensorParse.InstalledAt = DateTime.UtcNow;
    }

    var sensor = await sensorRepository.CreateAsync(newSensorParse);

    return TypedResults.Created($"/risk-area/${sensor.Area.Id}/sensors/${sensor.Id}",mapper.Map<SensorDto>(sensor));
})
.WithSummary("Registra um novo sensor na área de risco especificada.")
.WithDescription("""
Registra um novo sensor no sistema na área de risco existente.
""");

sensorsGroup.MapPut("/{id}", async Task<Results<Ok<SensorDto>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    ISensorRepository sensorRepository,
    IMapper mapper,
    string areaId,
    string id,
    SensorRequestDto updatedSensor
) => {
    if (string.IsNullOrWhiteSpace(areaId) || !Regex.IsMatch(areaId, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "O areaId nao segue um formato de CUID2 valido."));
    }

    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "O id nao segue um formato de CUID2 valido."));
    }

    var riskArea = await riskAreaRepository.FindAsync(areaId);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Área de risco com id {areaId} nao encontrada"));
    }

    var sensor = await sensorRepository.FindAsync(id);

    if (sensor is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Sensor com id {id} nao encontrado"));
    }

    if (updatedSensor.InstalledAt is null) {
        updatedSensor.InstalledAt = sensor.InstalledAt;
    }

    mapper.Map(updatedSensor, sensor);
    await sensorRepository.UpdateAsync();

    return TypedResults.Ok(mapper.Map<SensorDto>(sensor));
})
.WithSummary("Autaliza um sensor na área de risco especificada.")
.WithDescription("""
Atualiza um sensor no sistema na área de risco existente.
""");


app.Run();
