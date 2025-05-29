namespace Simapd.Models
{
    public class HealthCheck {
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }

        public HealthCheck(string status, DateTime timestamp) {
            this.Status = status;
            this.Timestamp = timestamp;
        }
    }
}
