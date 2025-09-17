// IUuIS_PZ2/ViewModels/GraphViewModel.cs
using IUuIS_PZ2.Interface;
using IUuIS_PZ2.Models;
using IUuIS_PZ2.Services;
using IUuIS_PZ2.Utils;
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
            var pm = new PlotModel { PlotAreaBorderColor = OxyColor.FromRgb(230, 230, 230) };

            // Uzimamo poslednje 4 merenja i mapiramo na t0..t3
            var data = ReadMeasurements(_selectedEntity?.Id).TakeLast(4).ToList();

            var x = new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time(t)",
                Key = "timeAxis",
                GapWidth = 0.2,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.FromRgb(238, 238, 238),
                MinorGridlineStyle = LineStyle.None
            };
            for (int i = 0; i < data.Count; i++) x.Labels.Add($"t{i}");
            pm.Axes.Add(x);

            // Y skala (MW) – krugovi stoje na srednjoj liniji (Y=3), grid lagan
            pm.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "MW",
                Minimum = 0,
                Maximum = 6,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.FromRgb(238, 238, 238),
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = OxyColor.FromRgb(245, 245, 245)
            });

            var valid = new ScatterSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColor.FromRgb(220, 220, 220),
                MarkerStroke = OxyColors.Gray,
                MarkerStrokeThickness = 1,
                XAxisKey = "timeAxis"
            };
            var invalid = new ScatterSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColors.Transparent,
                MarkerStroke = OxyColors.DimGray,
                MarkerStrokeThickness = 1,
                XAxisKey = "timeAxis"
            };

            for (int i = 0; i < data.Count; i++)
            {
                var r = data[i];

                // Poluprečnik ∝ vrednosti (1..5 MW) → veći krug za veću vrednost
                var size = Math.Clamp((r.Value - 1) / 4.0, 0, 1); // 0..1
                var markerSize = 16 + size * 40;                  // 16..56 px

                // Centar svih krugova na horizontalnoj liniji (Y=3)
                var p = new ScatterPoint(i, 3, markerSize);

                if (r.IsValid) valid.Points.Add(p);
                else invalid.Points.Add(p);
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
                       .ToList();
        }
    }
}