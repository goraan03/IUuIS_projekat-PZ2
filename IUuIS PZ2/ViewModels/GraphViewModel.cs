using IUuIS_PZ2.Models;
using IUuIS_PZ2.Utils;
using IUuIS_PZ2.Interface;
using IUuIS_PZ2.ViewModels;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IUuIS_PZ2.ViewModels
{
    public class GraphViewModel : ObservableObject
    {
        private readonly ILogService _log;
        private readonly EntitiesViewModel _entitiesVM;

        public IList<DerEntity> Entities => _entitiesVM.Entities;

        private DerEntity? _selectedEntity;
        public DerEntity? SelectedEntity
        {
            get => _selectedEntity;
            set { if (Set(ref _selectedEntity, value)) BuildPlot(); }
        }

        private PlotModel _plot = new();
        public PlotModel Plot { get => _plot; private set => Set(ref _plot, value); }

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
            var pm = new PlotModel();

            pm.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "HH:mm:ss",
                Title = "Time",
                IntervalType = DateTimeIntervalType.Seconds,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
                MinorGridlineStyle = LineStyle.Dot
            });
            pm.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "MW",
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
                MinorGridlineStyle = LineStyle.Dot
            });

            // Dve serije: valid (puni sivi), invalid (prazan krug)
            var valid = new ScatterSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColor.FromRgb(220, 220, 220),
                MarkerStroke = OxyColors.Gray,
                MarkerStrokeThickness = 1
            };
            var invalid = new ScatterSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColors.Transparent,
                MarkerStroke = OxyColors.DimGray,
                MarkerStrokeThickness = 1
            };

            foreach (var r in ReadMeasurements(_selectedEntity?.Id))
            {
                // G3: poluprečnik ∝ vrednosti
                var size = Math.Clamp(r.Value * 6.0, 8, 40);
                var x = DateTimeAxis.ToDouble(r.Timestamp);
                var point = new ScatterPoint(x, r.Value, size);

                if (r.IsValid) valid.Points.Add(point);
                else invalid.Points.Add(point);
            }

            pm.Series.Add(valid);
            pm.Series.Add(invalid);
            Plot = pm;
        }

        private IEnumerable<MeasurementRecord> ReadMeasurements(int? entityId)
        {
            if (entityId is null || !File.Exists(_log.LogPath))
                return Enumerable.Empty<MeasurementRecord>();

            return File.ReadLines(_log.LogPath)
                       .Skip(1)
                       .Select(line =>
                       {
                           var p = line.Split(';');
                           if (p.Length < 4) return null;
                           if (!int.TryParse(p[1], out var id) || id != entityId.Value) return null;
                           if (!DateTime.TryParse(p[0], null, DateTimeStyles.RoundtripKind, out var ts)) return null;
                           if (!double.TryParse(p[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var val)) return null;
                           var isValid = bool.TryParse(p[3], out var b) && b;
                           return new MeasurementRecord { Timestamp = ts.ToLocalTime(), EntityId = id, Value = val, IsValid = isValid };
                       })
                       .Where(x => x != null)!
                       .Cast<MeasurementRecord>()
                       .TakeLast(60)
                       .ToList();
        }
    }
}