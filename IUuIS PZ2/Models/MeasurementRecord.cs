namespace IUuIS_PZ2.Models
{
    public class MeasurementRecord
    {
        public DateTime Timestamp { get; set; }
        public int EntityId { get; set; }
        public double Value { get; set; }
        public bool IsValid { get; set; }
    }
}
