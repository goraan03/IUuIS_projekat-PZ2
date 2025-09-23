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

        // prikaz invalid tačaka (checkbox)
        private bool _showInvalid = true;
        public bool ShowInvalid
        {
            get => _showInvalid;
            set { if (Set(ref _showInvalid, value)) BuildPlot(); }
        }

        private PlotModel _plot = new();
        public PlotModel Plot { get => _plot; private set => Set(ref _plot, value); }

        public RelayCommand RefreshCommand { get; }

        public GraphViewModel(ILogService log, EntitiesViewModel entitiesVM)
        {
            _log = log;
            _entitiesVM = entitiesVM;
            _selectedEntity = Entities.FirstOrDefault();

            // auto-refresh grafa kada stigne merenje za izabran entitet
            _entitiesVM.MeasurementArrived += id =>
            {
                if (SelectedEntity?.Id == id) BuildPlot();
            };

            RefreshCommand = new RelayCommand(_ => BuildPlot());
            BuildPlot();
        }

        private void BuildPlot()
        {
            var pm = new PlotModel { PlotAreaBorderColor = OxyColor.FromRgb(230, 230, 230) };

            // poslednje 4 merenja (wireframe t0..t3)
            var data = ReadMeasurements(SelectedEntity?.Id).TakeLast(4).ToList();

            var x = new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time(t)",
                Key = "timeAxis",
                GapWidth = 0.2,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.FromRgb(238, 238, 238)
            };
            for (int i = 0; i < data.Count; i++) x.Labels.Add($"t{i}");
            pm.Axes.Add(x);

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
                // poluprečnik ∝ vrednosti 1..5  → 16..56px
                var t = Math.Clamp((r.Value - 1) / 4.0, 0, 1);
                var size = 16 + t * 40;

                // svi mehuri u visini Y=3 radi “horizontalnog” izgleda iz wireframe-a
                var point = new ScatterPoint(i, 3, size);

                if (r.IsValid) valid.Points.Add(point);
                else if (ShowInvalid) invalid.Points.Add(point);
            }

            pm.Series.Add(valid);
            if (ShowInvalid) pm.Series.Add(invalid);

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