using System;
using System.Collections.Generic;
using IUuIS_PZ2.Models;
using IUuIS_PZ2.Models;

using Timer = System.Timers.Timer;

namespace IUuIS_PZ2.Services
{
    public class MeasurementSimulator
    {
        private readonly Timer _timer;
        private readonly Random _rnd = new();
        private readonly Func<IReadOnlyList<DerEntity>> _entitiesProvider;

        public event EventHandler<(int entityId, double value)>? MeasurementArrived;

        public MeasurementSimulator(Func<IReadOnlyList<DerEntity>> entitiesProvider, double intervalMs = 1200)
        {
            _entitiesProvider = entitiesProvider;
            _timer = new Timer(intervalMs) { AutoReset = true };
            _timer.Elapsed += (_, __) => Tick();
        }

        private void Tick()
        {
            var list = _entitiesProvider();
            if (list == null || list.Count == 0) return;

            // Nasumicno izaberi entitet i generisi vrednost
            var ent = list[_rnd.Next(list.Count)];

            // T4: validno 1–5 MW; povremeno posalji i invalid
            var val = 1 + _rnd.NextDouble() * 4; // 1..5
            if (_rnd.NextDouble() < 0.2)
                val = _rnd.NextDouble() < 0.5 ? 0.5 : 5.8; // invalid vrednosti

            MeasurementArrived?.Invoke(this, (ent.Id, val));
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();
        public void ChangeInterval(double ms) => _timer.Interval = ms;
    }
}