using IUuIS_PZ2.Interface;
using System.IO;

namespace IUuIS_PZ2.Services
{
    public class LogService : ILogService
    {
        public string LogPath { get; }
        public LogService(string? path = null)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            LogPath = path ?? Path.Combine(baseDir, "log.txt");
            if (!File.Exists(LogPath)) File.WriteAllText(LogPath, "ts;entityId;value;valid\n");
        }
        public void AppendMeasurement(DateTime ts, int entityId, double value, bool isValid)
            => File.AppendAllText(LogPath, $"{ts:O};{entityId};{value:F3};{isValid}\n");
    }
}
