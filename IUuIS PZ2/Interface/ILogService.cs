namespace IUuIS_PZ2.Interface
{
    public interface ILogService
    {
        void AppendMeasurement(DateTime ts, int entityId, double value, bool isValid);
        string LogPath { get; }
    }
}
