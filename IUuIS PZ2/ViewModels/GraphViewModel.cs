using IUuIS_PZ2.Interface;
using IUuIS_PZ2.Models;
using IUuIS_PZ2.Utils;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IUuIS_PZ2.ViewModels
{
    public class GraphViewModel : ObservableObject
    {
        private readonly ILogService _log;
        private readonly EntitiesViewModel _entitiesVM;

        public IList<DerEntity> Entities => _entitiesVM.Entities;

        private DerEntity? _selectedEntity;
        public DerEntity? SelectedEntity { get => _selectedEntity; set { if (Set(ref _selectedEntity, value)) BuildPlot(); } }

        private PlotModel _plot = new();
        public PlotModel Plot { get => _plot; set => Set(ref _plot, value); }

        public RelayCommand RefreshCommand { get; }

        public GraphViewModel(ILogService log, EntitiesViewModel entitiesVM)
        {
            _log = log;
            _entitiesVM = entitiesVM;
            _selectedEntity = Entities.FirstOrDefault();
            RefreshCommand = new RelayCommand(_ => BuildPlot());
            BuildPlot();
        }

        private void BuildPlot()
        {
            var pm = new PlotModel { PlotAreaBorderColor = OxyColors.LightGray };
            pm.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "HH:mm:ss", Title = "Time", IntervalType = DateTimeIntervalType.Seconds, MinorGridlineStyle = LineStyle.Dot, MajorGridlineStyle = LineStyle.Solid, MajorGridlineColor = OxyColor.FromRgb(230, 230, 230) });
            pm.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "MW", MinorGridlineStyle = LineStyle.Dot, MajorGridlineStyle = LineStyle.Solid, MajorGridlineColor = OxyColor.FromRgb(230, 230, 230) });

            var s = new ScatterSeries { MarkerType = MarkerType.Circle, MarkerStroke = OxyColors.Gray, MarkerFill = OxyColor.FromRgb(220, 220, 220) };

            foreach (var r in ReadMeasurements(_selectedEntity?.Id))
            {
                // G3: poluprecnik ∝ vrednosti
                var size = Math.Clamp(r.Value * 6.0, 8, 40);
                var color = r.IsValid ? OxyColor.FromRgb(220, 220, 220) : OxyColors.Transparent;
                var stroke = r.IsValid ? OxyColors.Gray : OxyColors.DimGray;
                s.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(r.Timestamp), r.Value, size, 0, color) { MarkerStroke = stroke });
            }

            pm.Series.Add(s);
            Plot = pm;
        }

        private IEnumerable<MeasurementRecord> ReadMeasurements(int? entityId)
        {
            if (entityId is null || !File.Exists(_log.LogPath)) return Enumerable.Empty<MeasurementRecord>();
            return File.ReadLines(_log.LogPath)
                       .Skip(1)
                       .Select(line =>
                       {
                           var p = line.Split(';');
                           if (p.Length < 4) return null;
                           if (!int.TryParse(p[1], out var id) || id != entityId) return null;
                           if (!DateTime.TryParse(p[0], null, DateTimeStyles.RoundtripKind, out var ts)) return null;
                           if (!double.TryParse(p[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return null;
                           var isValid = bool.TryParse(p[3], out var b) && b;
                           return new MeasurementRecord { Timestamp = ts.ToLocalTime(), EntityId = id, Value = v, IsValid = isValid };
                       })
                       .Where(x => x != null)!
                       .Cast<MeasurementRecord>()
                       .TakeLast(60)
                       .ToList();
        }
    }
}
