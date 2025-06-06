using Simapd.Models;
using Simapd.Repositories;
using System.Text.Json;

namespace Simapd.Services
{
    public interface IMeasurementAnalysisService
    {
        Task<Alert?> AnalyzeAndGenerateAlertAsync(Measurement newMeasurement);
    }

    public class MeasurementAnalysisService : IMeasurementAnalysisService
    {
        private readonly IMeasurementRepository _measurementRepository;
        private readonly IAlertRepository _alertRepository;
        private readonly IRiskAreaRepository _riskAreaRepository;

        public MeasurementAnalysisService(
            IMeasurementRepository measurementRepository,
            IAlertRepository alertRepository,
            IRiskAreaRepository riskAreaRepository)
        {
            _measurementRepository = measurementRepository;
            _alertRepository = alertRepository;
            _riskAreaRepository = riskAreaRepository;
        }

        public async Task<Alert?> AnalyzeAndGenerateAlertAsync(Measurement newMeasurement)
        {
            var recentMeasurements = await _measurementRepository.ListPagedAsync(1, 100, newMeasurement.AreaId, null);
            
            var cutoffTime = newMeasurement.MeasuredAt.AddHours(-24);
            var relevantMeasurements = recentMeasurements.Data
                .Where(m => m.MeasuredAt >= cutoffTime && m.Id != newMeasurement.Id)
                .ToList();

            var riskAssessment = AnalyzeCombinedRisk(newMeasurement, relevantMeasurements);

            if (riskAssessment.ShouldGenerateAlert)
            {
                var area = await _riskAreaRepository.FindAsync(newMeasurement.AreaId);
                
                var alert = new Alert
                {
                    Message = riskAssessment.AlertMessage,
                    Level = riskAssessment.AlertLevel,
                    Origin = AlertOrigin.AUTOMATIC,
                    EmmitedAt = DateTime.UtcNow,
                    AreaId = newMeasurement.AreaId,
                    Area = area!
                };

                return await _alertRepository.CreateAsync(alert);
            }

            return null;
        }

        private RiskAssessmentResult AnalyzeCombinedRisk(Measurement newMeasurement, List<Measurement> recentMeasurements)
        {
            var result = new RiskAssessmentResult();

            var measurementsByType = recentMeasurements
                .GroupBy(m => m.type)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.MeasuredAt).ToList());

            if (newMeasurement.type == MeasurementType.RAIN)
            {
                result = AnalyzeRainRisk(newMeasurement, measurementsByType);
            }
            else if (newMeasurement.type == MeasurementType.MOVEMENT)
            {
                result = AnalyzeMovementRisk(newMeasurement, measurementsByType);
            }
            else if (newMeasurement.type == MeasurementType.SOIL_MOISTURE)
            {
                result = AnalyzeSoilMoistureRisk(newMeasurement, measurementsByType);
            }

            return result;
        }

        private RiskAssessmentResult AnalyzeRainRisk(
            Measurement rainMeasurement, 
            Dictionary<MeasurementType, List<Measurement>> measurementsByType)
        {
            var result = new RiskAssessmentResult();
            var rainValue = ExtractRainValue(rainMeasurement.value);

            if (measurementsByType.ContainsKey(MeasurementType.SOIL_MOISTURE))
            {
                var recentSoilMoisture = measurementsByType[MeasurementType.SOIL_MOISTURE]
                    .FirstOrDefault(m => (rainMeasurement.MeasuredAt - m.MeasuredAt).TotalHours <= 6);

                if (recentSoilMoisture != null)
                {
                    var soilValue = ExtractSoilMoistureValue(recentSoilMoisture.value);
                    
                    if (rainMeasurement.RiskLevel >= RiskLevel.MEDIUM && 
                        recentSoilMoisture.RiskLevel >= RiskLevel.HIGH)
                    {
                        result.ShouldGenerateAlert = true;
                        result.AlertLevel = RiskLevel.HIGH;
                        result.AlertMessage = $"ALERTA CRÍTICO: Combinação de chuva (nível: {rainValue}) com solo saturado (nível: {soilValue}) detectada. Risco elevado de deslizamento.";
                    }
                    else if (rainMeasurement.RiskLevel >= RiskLevel.HIGH)
                    {
                        result.ShouldGenerateAlert = true;
                        result.AlertLevel = RiskLevel.MEDIUM;
                        result.AlertMessage = $"ALERTA: Chuva intensa (nível: {rainValue}) detectada em área com umidade do solo elevada (nível: {soilValue}).";
                    }
                }
            }
            else if (rainMeasurement.RiskLevel >= RiskLevel.CRITICAL)
            {
                result.ShouldGenerateAlert = true;
                result.AlertLevel = RiskLevel.HIGH;
                result.AlertMessage = $"ALERTA: Chuva crítica detectada (nível: {rainValue}). Monitorar condições do terreno.";
            }

            return result;
        }

        private RiskAssessmentResult AnalyzeMovementRisk(
            Measurement movementMeasurement, 
            Dictionary<MeasurementType, List<Measurement>> measurementsByType)
        {
            var result = new RiskAssessmentResult();
            var movementInfo = ExtractMovementValue(movementMeasurement.value);

            if (movementMeasurement.RiskLevel >= RiskLevel.MEDIUM)
            {
                var hasAdverseConditions = false;
                var context = "";

                if (measurementsByType.ContainsKey(MeasurementType.RAIN))
                {
                    var recentRain = measurementsByType[MeasurementType.RAIN]
                        .FirstOrDefault(m => (movementMeasurement.MeasuredAt - m.MeasuredAt).TotalHours <= 12 && m.RiskLevel >= RiskLevel.MEDIUM);
                    
                    if (recentRain != null)
                    {
                        hasAdverseConditions = true;
                        var rainValue = ExtractRainValue(recentRain.value);
                        context += $" Chuva recente detectada (nível: {rainValue}).";
                    }
                }

                result.ShouldGenerateAlert = true;
                result.AlertLevel = hasAdverseConditions ? RiskLevel.CRITICAL : RiskLevel.HIGH;
                result.AlertMessage = $"MOVIMENTO DETECTADO: {movementInfo}.{context} EVACUAR ÁREA IMEDIATAMENTE.";
            }

            return result;
        }

        private RiskAssessmentResult AnalyzeSoilMoistureRisk(
            Measurement soilMoistureMeasurement, 
            Dictionary<MeasurementType, List<Measurement>> measurementsByType)
        {
            var result = new RiskAssessmentResult();
            var soilValue = ExtractSoilMoistureValue(soilMoistureMeasurement.value);

            if (soilMoistureMeasurement.RiskLevel >= RiskLevel.CRITICAL)
            {
                if (measurementsByType.ContainsKey(MeasurementType.RAIN))
                {
                    var recentRain = measurementsByType[MeasurementType.RAIN]
                        .FirstOrDefault(m => Math.Abs((soilMoistureMeasurement.MeasuredAt - m.MeasuredAt).TotalHours) <= 3 && m.RiskLevel >= RiskLevel.MEDIUM);
                    
                    if (recentRain != null)
                    {
                        var rainValue = ExtractRainValue(recentRain.value);
                        result.ShouldGenerateAlert = true;
                        result.AlertLevel = RiskLevel.HIGH;
                        result.AlertMessage = $"ALERTA: Solo saturado (nível: {soilValue}) com chuva recente (nível: {rainValue}). Risco de instabilidade.";
                    }
                }
                
                if (!result.ShouldGenerateAlert)
                {
                    result.ShouldGenerateAlert = true;
                    result.AlertLevel = RiskLevel.MEDIUM;
                    result.AlertMessage = $"ALERTA: Solo em estado crítico (nível: {soilValue}). Monitorar evolução.";
                }
            }

            return result;
        }

        private int ExtractRainValue(JsonElement jsonValue)
        {
            try
            {
                if (jsonValue.TryGetProperty("rainLevel", out var rainLevel))
                {
                    if (rainLevel.TryGetInt32(out var intValue))
                        return intValue;
                    
                    if (rainLevel.TryGetDouble(out var doubleValue))
                        return (int)doubleValue;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private int ExtractSoilMoistureValue(JsonElement jsonValue)
        {
            try
            {
                if (jsonValue.TryGetProperty("moistureLevel", out var moistureLevel))
                {
                    if (moistureLevel.TryGetInt32(out var intValue))
                        return intValue;
                    
                    if (moistureLevel.TryGetDouble(out var doubleValue))
                        return (int)doubleValue;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private string ExtractMovementValue(JsonElement jsonValue)
        {
            try
            {
                var accelerationMagnitude = 0.0;
                var rotationMagnitude = 0.0;

                if (jsonValue.TryGetProperty("acceleration", out var acceleration))
                {
                    if (acceleration.TryGetProperty("magnitude", out var accMag))
                    {
                        accMag.TryGetDouble(out accelerationMagnitude);
                    }
                }

                if (jsonValue.TryGetProperty("rotation", out var rotation))
                {
                    if (rotation.TryGetProperty("magnitude", out var rotMag))
                    {
                        rotMag.TryGetDouble(out rotationMagnitude);
                    }
                }

                return $"Aceleração: {accelerationMagnitude:F2}, Rotação: {rotationMagnitude:F2}";
            }
            catch
            {
                return "Movimento detectado";
            }
        }
    }

    public class RiskAssessmentResult
    {
        public bool ShouldGenerateAlert { get; set; } = false;
        public RiskLevel AlertLevel { get; set; } = RiskLevel.LOW;
        public string AlertMessage { get; set; } = "";
    }
} 