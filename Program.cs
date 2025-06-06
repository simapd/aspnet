using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Simapd.Models;
using Simapd.Dtos;
using Simapd.Repositories;
using Simapd.Services;
using AutoMapper;
using Simapd.Profiles;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
builder.Services.AddDbContext<SimapdDb>(opt
    => opt.UseNpgsql(dbConnectionString));

builder.Services.AddAutoMapper(typeof(RiskAreaProfile));
builder.Services.AddAutoMapper(typeof(SensorProfile));
builder.Services.AddAutoMapper(typeof(AlertProfile));
builder.Services.AddAutoMapper(typeof(MeasurementProfile));

builder.Services.AddScoped<IRiskAreaRepository, RiskAreaRepository>();
builder.Services.AddScoped<ISensorRepository, SensorRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IMeasurementRepository, MeasurementRepository>();
builder.Services.AddScoped<IMeasurementAnalysisService, MeasurementAnalysisService>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

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
Verifies the application's health status and returns basic operational information.

Response Fields:
- Status (string): Current application status ("Healthy")
- Timestamp (DateTime): Exact UTC date/time when the check was performed

Example Response:
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

Possible Failures:
- 500 Internal Server Error: Critical application failure (Unreachable)

Example Error Response:
```json
{
  "statusCode": 500,
  "message": "Internal server error occurred"
}
```

Recommended Usage:
This endpoint should be called by load balancers, monitoring tools (Kubernetes liveness/readiness probes),
and alerting systems to verify that the application is functioning properly.
""")
.WithSummary("Application health check endpoint")
.WithTags("Health", "Monitoring", "System")
.ProducesProblem(StatusCodes.Status500InternalServerError)
.AllowAnonymous()
.CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30)).Tag("health-check"));

var riskAreaGroup = app.MapGroup("/risk-areas")
    .WithTags("Risk Areas")
    .WithDescription("Endpoints for managing risk areas in the environmental monitoring system");

riskAreaGroup.MapGet("/{id}", async Task<Results<Ok<RiskAreaDto>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IMapper mapper,
    string id
) => {
    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The id does not follow a valid CUID2 format."));
    }

    var riskArea = await riskAreaRepository.FindAsync(id);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Risk area with id {id} not found"));
    }

    return TypedResults.Ok(mapper.Map<RiskAreaDto>(riskArea));
})
.WithSummary("Get a specific risk area by ID")
.WithDescription("""
Retrieves detailed information about a specific risk area using its unique identifier.

Path Parameters:
- id (string): The unique CUID2 identifier of the risk area (20-32 alphanumeric characters)

Example Request:
```
GET /risk-areas/k7u1v2w3x4y5z6a7b8c9d0
```

Example Response (200 OK):
```json
{
  "id": "k7u1v2w3x4y5z6a7b8c9d0",
  "name": "Nova Friburgo - Região Serrana RJ",
  "latitude": -22.2816,
  "longitude": -42.5311
}
```

Example Error Response (400 Bad Request):
```json
{
  "statusCode": 400,
  "message": "The id does not follow a valid CUID2 format."
}
```

Example Error Response (404 Not Found):
```json
{
  "statusCode": 404,
  "message": "Risk area with id invalid-id not found"
}
```

Response Codes:
- 200 OK: Returns the risk area data (RiskAreaDto)
- 400 Bad Request: Invalid CUID2 format for the provided ID
- 404 Not Found: Risk area with the specified ID does not exist
- 500 Internal Server Error: Unexpected server error
""");

riskAreaGroup.MapGet("/", async Task<Results<Ok<PagedResponseDto<RiskAreaDto>>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IMapper mapper,
    int pageNumber = 1,
    int pageSize = 10
) => {
    if (pageNumber <= 0) {
        return TypedResults.BadRequest(new ErrorResponse(400, "Page number must be greater than zero."));
    }

    if (pageSize <= 0) {
        return TypedResults.BadRequest(new ErrorResponse(400, "Page size must be greater than zero."));
    }

    var riskAreas = await riskAreaRepository.ListPagedAsync(pageNumber, pageSize);

    return TypedResults.Ok(mapper.Map<PagedResponseDto<RiskAreaDto>>(riskAreas));
})
.WithSummary("Get paginated list of risk areas")
.WithDescription("""
Retrieves a paginated list of all risk areas registered in the system.

Query Parameters:
- pageNumber (optional): Page number to retrieve. Must be greater than zero. Default: 1
- pageSize (optional): Number of items per page. Must be greater than zero. Default: 10

Example Request:
```
GET /risk-areas?pageNumber=1&pageSize=5
```

Example Response (200 OK):
```json
{
  "pageNumber": 1,
  "pageSize": 5,
  "totalPages": 3,
  "totalRecords": 15,
  "data": [
    {
      "id": "k7u1v2w3x4y5z6a7b8c9d0",
      "name": "Nova Friburgo - Região Serrana RJ",
      "latitude": -22.2816,
      "longitude": -42.5311
    },
    {
      "id": "m9n8o7p6q5r4s3t2u1v0w9",
      "name": "Petrópolis - Serra dos Órgãos RJ",
      "latitude": -22.5053,
      "longitude": -43.1754
    }
  ]
}
```

Example Error Response (400 Bad Request):
```json
{
  "statusCode": 400,
  "message": "Page number must be greater than zero."
}
```

Response Codes:
- 200 OK: Returns paginated risk areas data (PagedResponseDto<RiskAreaDto>)
- 400 Bad Request: Invalid pageNumber or pageSize (less than or equal to zero)
- 500 Internal Server Error: Unexpected server error

Response Structure:
The response includes pagination metadata (total count, current page, total pages) and the risk area data.
""");

riskAreaGroup.MapPost("/", async Task<Results<Created<RiskAreaDto>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IMapper mapper,
    RiskAreaRequestDto newRiskArea
) => {
    var riskArea = await riskAreaRepository.CreateAsync(mapper.Map<RiskArea>(newRiskArea));

    return TypedResults.Created($"/risk-areas/{riskArea.Id}",mapper.Map<RiskAreaDto>(riskArea));
})
.WithSummary("Create a new risk area")
.WithDescription("""
Creates a new risk area in the environmental monitoring system.

Request Body:
- RiskAreaRequestDto: Contains the risk area information to be created

Example Request:
```json
{
  "name": "Cubatão - Polo Industrial SP",
  "latitude": -23.8951,
  "longitude": -46.4248
}
```

Example Response (201 Created):
```json
{
  "id": "k7u1v2w3x4y5z6a7b8c9d0",
  "name": "Cubatão - Polo Industrial SP",
  "latitude": -23.8951,
  "longitude": -46.4248
}
```

Example Error Response (400 Bad Request):
```json
{
  "statusCode": 400,
  "message": "Validation error: Name is required"
}
```

Response Codes:
- 201 Created: Risk area successfully created, returns the created risk area data
- 400 Bad Request: Invalid request data or validation errors
- 500 Internal Server Error: Unexpected server error

The response includes a Location header with the URL of the newly created resource.
""");

riskAreaGroup.MapPut("/{id}", async Task<Results<Ok<RiskAreaDto>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IMapper mapper,
    string id,
    RiskAreaRequestDto updatedRiskArea
) => {
    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The id does not follow a valid CUID2 format."));
    }

    var riskArea = await riskAreaRepository.FindAsync(id);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Risk area with id {id} not found"));
    }

    mapper.Map(updatedRiskArea, riskArea);
    await riskAreaRepository.UpdateAsync();

    return TypedResults.Ok(mapper.Map<RiskAreaDto>(riskArea));
})
.WithSummary("Update an existing risk area")
.WithDescription("""
Updates an existing risk area with new information.

Path Parameters:
- id (string): The unique CUID2 identifier of the risk area to update

Request Body:
- RiskAreaRequestDto: Contains the updated risk area information

Example Request:
```
PUT /risk-areas/k7u1v2w3x4y5z6a7b8c9d0
```

```json
{
  "name": "Nova Friburgo - Região Serrana RJ (Atualizado)",
  "latitude": -22.2816,
  "longitude": -42.5311
}
```

Example Response (200 OK):
```json
{
  "id": "k7u1v2w3x4y5z6a7b8c9d0",
  "name": "Nova Friburgo - Região Serrana RJ (Atualizado)",
  "latitude": -22.2816,
  "longitude": -42.5311
}
```

Response Codes:
- 200 OK: Risk area successfully updated, returns the updated data
- 400 Bad Request: Invalid CUID2 format or validation errors
- 404 Not Found: Risk area with the specified ID does not exist
- 500 Internal Server Error: Unexpected server error
""");

riskAreaGroup.MapDelete("/{id}", async Task<Results<NoContent, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IMapper mapper,
    string id
) => {
    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The id does not follow a valid CUID2 format."));
    }

    var riskArea = await riskAreaRepository.FindAsync(id);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Risk area with id {id} not found"));
    }

    await riskAreaRepository.DeleteAsync(riskArea);

    return TypedResults.NoContent();
})
.WithSummary("Delete a risk area")
.WithDescription("""
Permanently deletes a risk area from the system.

Path Parameters:
- id (string): The unique CUID2 identifier of the risk area to delete

Example Request:
```
DELETE /risk-areas/k7u1v2w3x4y5z6a7b8c9d0
```

Example Response (204 No Content):
```
(Empty response body)
```

Response Codes:
- 204 No Content: Risk area successfully deleted
- 400 Bad Request: Invalid CUID2 format for the provided ID
- 404 Not Found: Risk area with the specified ID does not exist
- 500 Internal Server Error: Unexpected server error

Warning: This operation is irreversible and will also delete all associated sensors and alerts.
""");

var sensorsGroup = app.MapGroup("/risk-areas/{areaId}/sensors")
    .WithTags("Sensors")
    .WithDescription("Endpoints for managing environmental sensors within risk areas");

sensorsGroup.MapGet("/", async Task<Results<Ok<PagedResponseDto<SensorDto>>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    ISensorRepository sensorRepository,
    IMapper mapper,
    string areaId,
    int pageNumber = 1,
    int pageSize = 10
) => {
    if (pageNumber <= 0) {
        return TypedResults.BadRequest(new ErrorResponse(400, "Page number must be greater than zero."));
    }

    if (pageSize <= 0) {
        return TypedResults.BadRequest(new ErrorResponse(400, "Page size must be greater than zero."));
    }

    var area = await riskAreaRepository.FindAsync(areaId);

    if (area is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Risk area with id {areaId} not found"));
    }

    var sensors = await sensorRepository.ListPagedAsync(areaId, pageNumber, pageSize);

    return TypedResults.Ok(mapper.Map<PagedResponseDto<SensorDto>>(sensors));
})
.WithSummary("Get paginated list of sensors in a risk area")
.WithDescription("""
Retrieves a paginated list of all sensors deployed in a specific risk area.

Path Parameters:
- areaId (string): The unique CUID2 identifier of the risk area

Query Parameters:
- pageNumber (optional): Page number to retrieve. Must be greater than zero. Default: 1
- pageSize (optional): Number of items per page. Must be greater than zero. Default: 10

Example Request:
```
GET /risk-areas/k7u1v2w3x4y5z6a7b8c9d0/sensors?pageNumber=1&pageSize=3
```

Example Response (200 OK):
```json
{
  "pageNumber": 1,
  "pageSize": 3,
  "totalPages": 2,
  "totalRecords": 6,
  "data": [
    {
      "id": "s1e2n3s4o5r6a7b8c9d0e1",
      "description": "Sensor pluviométrico - monitoramento de chuvas",
      "installedAt": "2024-01-10T08:00:00.000Z",
      "maintainedAt": "2024-01-15T14:30:00.000Z",
      "area": {
        "id": "k7u1v2w3x4y5z6a7b8c9d0",
        "name": "Nova Friburgo - Região Serrana RJ",
        "latitude": -22.2816,
        "longitude": -42.5311
      }
    }
  ]
}
```

Response Codes:
- 200 OK: Returns paginated sensor data (PagedResponseDto<SensorDto>)
- 400 Bad Request: Invalid pageNumber, pageSize, or areaId format
- 404 Not Found: Risk area with the specified ID does not exist
- 500 Internal Server Error: Unexpected server error
""");

sensorsGroup.MapGet("/{id}", async Task<Results<Ok<SensorDto>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    ISensorRepository sensorRepository,
    IMapper mapper,
    string areaId,
    string id
) => {
    if (string.IsNullOrWhiteSpace(areaId) || !Regex.IsMatch(areaId, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The areaId does not follow a valid CUID2 format."));
    }

    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The id does not follow a valid CUID2 format."));
    }

    var riskArea = await riskAreaRepository.FindAsync(areaId);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Risk area with id {areaId} not found"));
    }

    var sensor = await sensorRepository.FindAsync(id);

    return TypedResults.Ok(mapper.Map<SensorDto>(sensor));
})
.WithSummary("Get a specific sensor by ID")
.WithDescription("""
Retrieves detailed information about a specific sensor within a risk area.

Path Parameters:
- areaId (string): The unique CUID2 identifier of the risk area
- id (string): The unique CUID2 identifier of the sensor

Example Request:
```
GET /risk-areas/k7u1v2w3x4y5z6a7b8c9d0/sensors/s1e2n3s4o5r6a7b8c9d0e1
```

Example Response (200 OK):
```json
{
  "id": "s1e2n3s4o5r6a7b8c9d0e1",
  "description": "Sensor pluviométrico - monitoramento de chuvas",
  "installedAt": "2024-01-10T08:00:00.000Z",
  "maintainedAt": "2024-01-15T14:30:00.000Z",
  "area": {
    "id": "k7u1v2w3x4y5z6a7b8c9d0",
    "name": "Nova Friburgo - Região Serrana RJ",
    "latitude": -22.2816,
    "longitude": -42.5311
  }
}
```

Response Codes:
- 200 OK: Returns the sensor data (SensorDto)
- 400 Bad Request: Invalid CUID2 format for areaId or sensor id
- 404 Not Found: Risk area or sensor with the specified ID does not exist
- 500 Internal Server Error: Unexpected server error
""");

sensorsGroup.MapPost("/", async Task<Results<Created<SensorDto>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    ISensorRepository sensorRepository,
    IMapper mapper,
    string areaId,
    SensorRequestDto newSensor
) => {
    if (string.IsNullOrWhiteSpace(areaId) || !Regex.IsMatch(areaId, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The areaId does not follow a valid CUID2 format."));
    }

    var riskArea = await riskAreaRepository.FindAsync(areaId);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Risk area with id {areaId} not found"));
    }

    var newSensorParse = mapper.Map<Sensor>(newSensor);
    newSensorParse.AreaId = areaId;

    if (newSensor.InstalledAt is null) {
        newSensorParse.InstalledAt = DateTime.UtcNow;
    }

    var sensor = await sensorRepository.CreateAsync(newSensorParse);

    return TypedResults.Created($"/risk-areas/{sensor.Area.Id}/sensors/{sensor.Id}",mapper.Map<SensorDto>(sensor));
})
.WithSummary("Deploy a new sensor in a risk area")
.WithDescription("""
Creates and deploys a new environmental sensor in the specified risk area.

Path Parameters:
- areaId (string): The unique CUID2 identifier of the risk area where the sensor will be deployed

Request Body:
- SensorRequestDto: Contains the sensor configuration and metadata

Example Request:
```
POST /risk-areas/k7u1v2w3x4y5z6a7b8c9d0/sensors
```

```json
{
  "description": "Sensor pluviométrico - monitoramento de chuvas",
  "installedAt": "2024-01-10T08:00:00.000Z",
  "maintainedAt": null
}
```

Example Response (201 Created):
```json
{
  "id": "s1e2n3s4o5r6a7b8c9d0e1",
  "description": "Sensor pluviométrico - monitoramento de chuvas",
  "installedAt": "2024-01-10T08:00:00.000Z",
  "maintainedAt": null,
  "area": {
    "id": "k7u1v2w3x4y5z6a7b8c9d0",
    "name": "Nova Friburgo - Região Serrana RJ",
    "latitude": -22.2816,
    "longitude": -42.5311
  }
}
```

Response Codes:
- 201 Created: Sensor successfully created and deployed, returns the sensor data
- 400 Bad Request: Invalid areaId format or validation errors
- 404 Not Found: Risk area with the specified ID does not exist
- 500 Internal Server Error: Unexpected server error

Note: If InstalledAt is not provided, it will be automatically set to the current UTC timestamp.
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
        return TypedResults.BadRequest(new ErrorResponse(400, "The areaId does not follow a valid CUID2 format."));
    }

    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The id does not follow a valid CUID2 format."));
    }

    var riskArea = await riskAreaRepository.FindAsync(areaId);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Risk area with id {areaId} not found"));
    }

    var sensor = await sensorRepository.FindAsync(id);

    if (sensor is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Sensor with id {id} not found"));
    }

    if (updatedSensor.InstalledAt is null) {
        updatedSensor.InstalledAt = sensor.InstalledAt;
    }

    mapper.Map(updatedSensor, sensor);
    await sensorRepository.UpdateAsync();

    return TypedResults.Ok(mapper.Map<SensorDto>(sensor));
})
.WithSummary("Update an existing sensor configuration")
.WithDescription("""
Updates the configuration and metadata of an existing sensor in a risk area.

Path Parameters:
- areaId (string): The unique CUID2 identifier of the risk area
- id (string): The unique CUID2 identifier of the sensor to update

Request Body:
- SensorRequestDto: Contains the updated sensor configuration

Example Request:
```
PUT /risk-areas/k7u1v2w3x4y5z6a7b8c9d0/sensors/s1e2n3s4o5r6a7b8c9d0e1
```

```json
{
  "description": "Sensor pluviométrico calibrado - monitoramento intensivo",
  "maintainedAt": "2024-01-20T10:00:00.000Z"
}
```

Example Response (200 OK):
```json
{
  "id": "s1e2n3s4o5r6a7b8c9d0e1",
  "description": "Sensor pluviométrico calibrado - monitoramento intensivo",
  "installedAt": "2024-01-10T08:00:00.000Z",
  "maintainedAt": "2024-01-20T10:00:00.000Z",
  "area": {
    "id": "k7u1v2w3x4y5z6a7b8c9d0",
    "name": "Nova Friburgo - Região Serrana RJ",
    "latitude": -22.2816,
    "longitude": -42.5311
  }
}
```

Response Codes:
- 200 OK: Sensor successfully updated, returns the updated sensor data
- 400 Bad Request: Invalid CUID2 format for areaId or sensor id, or validation errors
- 404 Not Found: Risk area or sensor with the specified ID does not exist
- 500 Internal Server Error: Unexpected server error

Note: If InstalledAt is not provided in the request, the original installation timestamp is preserved.
""");

sensorsGroup.MapDelete("/{id}", async Task<Results<NoContent, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    ISensorRepository sensorRepository,
    IMapper mapper,
    string areaId,
    string id
) => {
    if (string.IsNullOrWhiteSpace(areaId) || !Regex.IsMatch(areaId, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The areaId does not follow a valid CUID2 format."));
    }

    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The id does not follow a valid CUID2 format."));
    }

    var riskArea = await riskAreaRepository.FindAsync(areaId);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Risk area with id {areaId} not found"));
    }

    var sensor = await sensorRepository.FindAsync(id);

    if (sensor is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Sensor with id {id} not found"));
    }

    await sensorRepository.DeleteAsync(sensor);

    return TypedResults.NoContent();
})
.WithSummary("Remove a sensor from a risk area")
.WithDescription("""
Permanently removes a sensor from the specified risk area.

Path Parameters:
- areaId (string): The unique CUID2 identifier of the risk area
- id (string): The unique CUID2 identifier of the sensor to remove

Example Request:
```
DELETE /risk-areas/k7u1v2w3x4y5z6a7b8c9d0/sensors/s1e2n3s4o5r6a7b8c9d0e1
```

Example Response (204 No Content):
```
(Empty response body)
```

Response Codes:
- 204 No Content: Sensor successfully removed
- 400 Bad Request: Invalid CUID2 format for areaId or sensor id
- 404 Not Found: Risk area or sensor with the specified ID does not exist
- 500 Internal Server Error: Unexpected server error

Warning: This operation is irreversible and will permanently delete all sensor data and associated readings.
""");

var alertsGroup = app.MapGroup("/risk-areas/{areaId}/alerts")
    .WithTags("Alerts")
    .WithDescription("Endpoints for managing environmental alerts and notifications within risk areas");

alertsGroup.MapGet("/{id}", async Task<Results<Ok<AlertDto>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IAlertRepository alertRepository,
    IMapper mapper,
    string areaId,
    string id
) => {
    if (string.IsNullOrWhiteSpace(areaId) || !Regex.IsMatch(areaId, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The areaId does not follow a valid CUID2 format."));
    }

    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The id does not follow a valid CUID2 format."));
    }

    var area = await riskAreaRepository.FindAsync(areaId);

    if (area is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Risk area with id {areaId} not found"));
    }

    var alert = await alertRepository.FindAsync(id);

    if (alert is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Alert with id {id} not found"));
    }

    return TypedResults.Ok(mapper.Map<AlertDto>(alert));
})
.WithSummary("Get a specific alert by ID")
.WithDescription("""
Retrieves detailed information about a specific environmental alert within a risk area.

Path Parameters:
- areaId (string): The unique CUID2 identifier of the risk area
- id (string): The unique CUID2 identifier of the alert

Example Request:
```
GET /risk-areas/k7u1v2w3x4y5z6a7b8c9d0/alerts/a1l2e3r4t5a6b7c8d9e0f1
```

Example Response (200 OK):
```json
{
  "id": "a1l2e3r4t5a6b7c8d9e0f1",
  "message": "Chuva intensa detectada - risco de deslizamento elevado",
  "level": "HIGH",
  "origin": "AUTOMATIC",
  "emmitedAt": "2024-01-15T15:30:00.000Z",
  "area": {
    "id": "k7u1v2w3x4y5z6a7b8c9d0",
    "name": "Nova Friburgo - Região Serrana RJ",
    "latitude": -22.2816,
    "longitude": -42.5311
  }
}
```

Alert Level Enum Values:
- LOW: Low priority alert
- MEDIUM: Medium priority alert requiring attention
- HIGH: High priority alert requiring immediate action

Alert Origin Enum Values:
- MANUAL: Alert created manually by an operator
- AUTOMATIC: Alert generated automatically by the system

Response Codes:
- 200 OK: Returns the alert data (AlertDto)
- 400 Bad Request: Invalid CUID2 format for areaId or alert id
- 404 Not Found: Risk area or alert with the specified ID does not exist
- 500 Internal Server Error: Unexpected server error
""");

alertsGroup.MapGet("/", async Task<Results<Ok<PagedResponseDto<AlertDto>>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IAlertRepository alertRepository,
    IMapper mapper,
    string areaId,
    int pageNumber = 1,
    int pageSize = 10
) => {
    if (pageNumber <= 0) {
        return TypedResults.BadRequest(new ErrorResponse(400, "Page number must be greater than zero."));
    }

    if (pageSize <= 0) {
        return TypedResults.BadRequest(new ErrorResponse(400, "Page size must be greater than zero."));
    }

    var area = await riskAreaRepository.FindAsync(areaId);

    if (area is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Risk area with id {areaId} not found"));
    }

    var alerts = await alertRepository.ListPagedAsync(areaId, pageNumber, pageSize);

    return TypedResults.Ok(mapper.Map<PagedResponseDto<AlertDto>>(alerts));
})
.WithSummary("Get paginated list of alerts in a risk area")
.WithDescription("""
Retrieves a paginated list of all environmental alerts generated in a specific risk area.

Path Parameters:
- areaId (string): The unique CUID2 identifier of the risk area

Query Parameters:
- pageNumber (optional): Page number to retrieve. Must be greater than zero. Default: 1
- pageSize (optional): Number of items per page. Must be greater than zero. Default: 10

Example Request:
```
GET /risk-areas/k7u1v2w3x4y5z6a7b8c9d0/alerts?pageNumber=1&pageSize=3
```

Example Response (200 OK):
```json
{
  "pageNumber": 1,
  "pageSize": 3,
  "totalPages": 2,
  "totalRecords": 5,
  "data": [
    {
      "id": "a1l2e3r4t5a6b7c8d9e0f1",
      "message": "Chuva intensa detectada - risco de deslizamento elevado",
      "level": "HIGH",
      "origin": "AUTOMATIC",
      "emmitedAt": "2024-01-15T15:30:00.000Z",
      "area": {
        "id": "k7u1v2w3x4y5z6a7b8c9d0",
        "name": "Nova Friburgo - Região Serrana RJ",
        "latitude": -22.2816,
        "longitude": -42.5311
      }
    },
    {
      "id": "b2c3d4e5f6g7h8i9j0k1l2",
      "message": "Inspeção preventiva agendada - período chuvoso",
      "level": "MEDIUM",
      "origin": "MANUAL",
      "emmitedAt": "2024-01-15T14:15:00.000Z",
      "area": {
        "id": "k7u1v2w3x4y5z6a7b8c9d0",
        "name": "Nova Friburgo - Região Serrana RJ",
        "latitude": -22.2816,
        "longitude": -42.5311
      }
    }
  ]
}
```

Alert Level Enum Values:
- LOW: Low priority alert
- MEDIUM: Medium priority alert requiring attention
- HIGH: High priority alert requiring immediate action

Alert Origin Enum Values:
- MANUAL: Alert created manually by an operator
- AUTOMATIC: Alert generated automatically by the system

Response Codes:
- 200 OK: Returns paginated alert data (PagedResponseDto<AlertDto>)
- 400 Bad Request: Invalid pageNumber, pageSize, or areaId format
- 404 Not Found: Risk area with the specified ID does not exist
- 500 Internal Server Error: Unexpected server error

The alerts are typically ordered by emission date, with the most recent alerts first.
""");

alertsGroup.MapPost("/", async Task<Results<Created<AlertDto>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IAlertRepository alertRepository,
    IMapper mapper,
    string areaId,
    AlertRequestDto newAlert
) => {
    if (string.IsNullOrWhiteSpace(areaId) || !Regex.IsMatch(areaId, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The areaId does not follow a valid CUID2 format."));
    }

    var riskArea = await riskAreaRepository.FindAsync(areaId);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Risk area with id {areaId} not found"));
    }

    var newAlertParse = mapper.Map<Alert>(newAlert);
    newAlertParse.AreaId = areaId;

    if (newAlert.EmmitedAt is null) {
        newAlertParse.EmmitedAt = DateTime.UtcNow;
    }

    var alert = await alertRepository.CreateAsync(newAlertParse);

    return TypedResults.Created($"/risk-areas/{alert.Area.Id}/alerts/{alert.Id}",mapper.Map<AlertDto>(alert));
})
.WithSummary("Create a new environmental alert")
.WithDescription("""
Creates a new environmental alert for the specified risk area.

Path Parameters:
- areaId (string): The unique CUID2 identifier of the risk area where the alert will be created

Request Body:
- AlertRequestDto: Contains the alert information including severity level, description, and origin

Example Request:
```
POST /risk-areas/k7u1v2w3x4y5z6a7b8c9d0/alerts
```

```json
{
  "message": "Alerta crítico: Volume pluviométrico acima do limite seguro",
  "level": "HIGH",
  "origin": "AUTOMATIC",
  "emmitedAt": "2024-01-15T15:30:00.000Z"
}
```

Example Response (201 Created):
```json
{
  "id": "a1l2e3r4t5a6b7c8d9e0f1",
  "message": "Alerta crítico: Volume pluviométrico acima do limite seguro",
  "level": "HIGH",
  "origin": "AUTOMATIC",
  "emmitedAt": "2024-01-15T15:30:00.000Z",
  "area": {
    "id": "k7u1v2w3x4y5z6a7b8c9d0",
    "name": "Nova Friburgo - Região Serrana RJ",
    "latitude": -22.2816,
    "longitude": -42.5311
  }
}
```

Alert Level Enum Values:
- LOW: Low priority alert
- MEDIUM: Medium priority alert requiring attention
- HIGH: High priority alert requiring immediate action

Alert Origin Enum Values:
- MANUAL: Alert created manually by an operator
- AUTOMATIC: Alert generated automatically by the system

Response Codes:
- 201 Created: Alert successfully created, returns the alert data
- 400 Bad Request: Invalid areaId format or validation errors
- 404 Not Found: Risk area with the specified ID does not exist
- 500 Internal Server Error: Unexpected server error

Note: If EmmitedAt is not provided, it will be automatically set to the current UTC timestamp.
""");

alertsGroup.MapPut("/{id}", async Task<Results<Ok<AlertDto>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IAlertRepository alertRepository,
    IMapper mapper,
    string areaId,
    string id,
    AlertRequestDto updatedAlert
) => {
    if (string.IsNullOrWhiteSpace(areaId) || !Regex.IsMatch(areaId, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The areaId does not follow a valid CUID2 format."));
    }

    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The id does not follow a valid CUID2 format."));
    }

    var riskArea = await riskAreaRepository.FindAsync(areaId);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Risk area with id {areaId} not found"));
    }

    var alert = await alertRepository.FindAsync(id);

    if (alert is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Alert with id {id} not found"));
    }

    if (updatedAlert.EmmitedAt is null) {
        updatedAlert.EmmitedAt = alert.EmmitedAt;
    }

    if (updatedAlert.Level is null) {
        updatedAlert.Level = alert.Level;
    }

    if (updatedAlert.Origin is null) {
        updatedAlert.Origin = alert.Origin;
    }

    mapper.Map(updatedAlert, alert);
    await alertRepository.UpdateAsync();

    return TypedResults.Ok(mapper.Map<AlertDto>(alert));
})
.WithSummary("Update an existing environmental alert")
.WithDescription("""
Updates an existing environmental alert with new information.

Path Parameters:
- areaId (string): The unique CUID2 identifier of the risk area
- id (string): The unique CUID2 identifier of the alert to update

Request Body:
- AlertRequestDto: Contains the updated alert information

Example Request:
```
PUT /risk-areas/k7u1v2w3x4y5z6a7b8c9d0/alerts/a1l2e3r4t5a6b7c8d9e0f1
```

```json
{
  "message": "Alerta crítico: Volume pluviométrico acima do limite seguro",
  "level": "HIGH",
  "origin": "AUTOMATIC"
}
```

Example Response (200 OK):
```json
{
  "id": "a1l2e3r4t5a6b7c8d9e0f1",
  "message": "Alerta crítico: Volume pluviométrico acima do limite seguro",
  "level": "HIGH",
  "origin": "AUTOMATIC",
  "emmitedAt": "2024-01-15T15:30:00.000Z",
  "area": {
    "id": "k7u1v2w3x4y5z6a7b8c9d0",
    "name": "Nova Friburgo - Região Serrana RJ",
    "latitude": -22.2816,
    "longitude": -42.5311
  }
}
```

Alert Level Enum Values:
- LOW: Low priority alert
- MEDIUM: Medium priority alert requiring attention
- HIGH: High priority alert requiring immediate action

Alert Origin Enum Values:
- MANUAL: Alert created manually by an operator
- AUTOMATIC: Alert generated automatically by the system

Response Codes:
- 200 OK: Alert successfully updated, returns the updated alert data
- 400 Bad Request: Invalid CUID2 format for areaId or alert id, or validation errors
- 404 Not Found: Risk area or alert with the specified ID does not exist
- 500 Internal Server Error: Unexpected server error

Note: If optional fields (EmmitedAt, Level, Origin) are not provided, the original values are preserved.
""");

alertsGroup.MapDelete("/{id}", async Task<Results<NoContent, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IRiskAreaRepository riskAreaRepository,
    IAlertRepository alertRepository,
    IMapper mapper,
    string areaId,
    string id
) => {
    if (string.IsNullOrWhiteSpace(areaId) || !Regex.IsMatch(areaId, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The areaId does not follow a valid CUID2 format."));
    }

    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The id does not follow a valid CUID2 format."));
    }

    var riskArea = await riskAreaRepository.FindAsync(areaId);

    if (riskArea is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Risk area with id {areaId} not found"));
    }

    var alert = await alertRepository.FindAsync(id);

    if (alert is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Alert with id {id} not found"));
    }

    await alertRepository.DeleteAsync(alert);

    return TypedResults.NoContent();
})
.WithSummary("Delete an environmental alert")
.WithDescription("""
Permanently deletes an environmental alert from the specified risk area.

Path Parameters:
- areaId (string): The unique CUID2 identifier of the risk area
- id (string): The unique CUID2 identifier of the alert to delete

Example Request:
```
DELETE /risk-areas/k7u1v2w3x4y5z6a7b8c9d0/alerts/a1l2e3r4t5a6b7c8d9e0f1
```

Example Response (204 No Content):
```
(Empty response body)
```

Response Codes:
- 204 No Content: Alert successfully deleted
- 400 Bad Request: Invalid CUID2 format for areaId or alert id
- 404 Not Found: Risk area or alert with the specified ID does not exist
- 500 Internal Server Error: Unexpected server error

Warning: This operation is irreversible and will permanently delete the alert from the system.
""");

var measurementGroup = app.MapGroup("/measurements")
    .WithTags("Measurements")
    .WithDescription("Endpoints for managing measurements in the environmental monitoring system");

measurementGroup.MapGet("/{id}", async Task<Results<Ok<MeasurementDto>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> (
    IMeasurementRepository measurementRepository,
    IMapper mapper,
    string id
) => {
    if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[a-z0-9]{20,32}$")) {
        return TypedResults.BadRequest(new ErrorResponse(400, "The id does not follow a valid CUID2 format."));
    }

    var measurement = await measurementRepository.FindAsync(id);

    if (measurement is null) {
        return TypedResults.NotFound(new ErrorResponse(404, $"Measurement with id {id} not found"));
    }

    return TypedResults.Ok(mapper.Map<MeasurementDto>(measurement));
})
.WithSummary("Get a specific measurement by ID")
.WithDescription(""" """);

measurementGroup.MapGet("/", async Task<Results<Ok<PagedResponseDto<MeasurementDto>>, BadRequest<ErrorResponse>>> (
    IMeasurementRepository measurementRepository,
    IMapper mapper,
    int pageNumber = 1,
    int pageSize = 10,
    string? areaId = null,
    string? sensorId = null
) => {
    if (pageNumber <= 0) {
        return TypedResults.BadRequest(new ErrorResponse(400, "Page number must be greater than zero."));
    }

    if (pageSize <= 0) {
        return TypedResults.BadRequest(new ErrorResponse(400, "Page size must be greater than zero."));
    }

    var measurements = await measurementRepository.ListPagedAsync(pageNumber, pageSize, areaId, sensorId);

    return TypedResults.Ok(mapper.Map<PagedResponseDto<MeasurementDto>>(measurements));
})
.WithSummary("Get paginated list of measurements")
.WithDescription(""" """);

measurementGroup.MapPost("/", async Task<Results<Created<MeasurementDto>, BadRequest<ErrorResponse>>> (
    IMeasurementRepository measurementRepository,
    IServiceScopeFactory serviceScopeFactory,
    IMapper mapper,
    MeasurementRequestDto newMeasurement
) => {
    if (newMeasurement.MeasuredAt is null) {
        newMeasurement.MeasuredAt = DateTime.UtcNow;
    }

    var measurement = await measurementRepository.CreateAsync(mapper.Map<Measurement>(newMeasurement));

    _ = Task.Run(async () => {
        using var scope = serviceScopeFactory.CreateScope();
        var analysisService = scope.ServiceProvider.GetRequiredService<IMeasurementAnalysisService>();
        try
        {
            await analysisService.AnalyzeAndGenerateAlertAsync(measurement);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro na análise automática: {ex.Message}");
        }
    });

    return TypedResults.Created($"/measurements/{measurement.Id}", mapper.Map<MeasurementDto>(measurement));
})
.WithSummary("Create a new measurement")
.WithDescription("""
Creates a new environmental measurement. The system automatically analyzes the measurement 
in background and may generate alerts based on combinations with recent measurements from the same area.

Request Body:
- MeasurementRequestDto: Contains the measurement data including type, value, risk level, sensor and area

Example Request:
```json
{
  "type": "RAIN",
  "value": 450,
  "riskLevel": "MEDIUM",
  "sensorId": "s1e2n3s4o5r6a7b8c9d0e1",
  "areaId": "k7u1v2w3x4y5z6a7b8c9d0",
  "measuredAt": "2024-01-15T15:30:00.000Z"
}
```

Note: Measurement values are numeric (0-1023) for RAIN and SOIL_MOISTURE types.

Response Codes:
- 201 Created: Measurement successfully created
- 400 Bad Request: Invalid request data or validation errors
- 500 Internal Server Error: Unexpected server error
""");

app.Run();
