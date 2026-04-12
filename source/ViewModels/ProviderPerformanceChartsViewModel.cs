using GameActivity.Services;
using GameActivity.Services.HardwareMonitoring.Models;
using LiveCharts;
using LiveCharts.Wpf;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameActivity.ViewModels
{
    /// <summary>
    /// Shared palette for provider lines and the global legend swatches.
    /// </summary>
    internal static class ProviderChartColors
    {
        private static readonly Color[] Palette =
        {
            Color.FromRgb(33, 150, 243),
            Color.FromRgb(156, 39, 176),
            Color.FromRgb(76, 175, 80),
            Color.FromRgb(255, 152, 0),
            Color.FromRgb(244, 67, 54),
            Color.FromRgb(0, 150, 136),
            Color.FromRgb(121, 85, 72),
            Color.FromRgb(63, 81, 181)
        };

        public static Brush CreateBrush(int index)
        {
            Color c = Palette[index % Palette.Length];
            var brush = new SolidColorBrush(c);
            brush.Freeze();
            return brush;
        }
    }

    /// <summary>
    /// Maps each chart metric to the provider name recorded in <see cref="HardwareMetrics.Source"/>
    /// after aggregation (same as logged session metrics).
    /// </summary>
    internal static class AggregatedMetricSourceResolver
    {
        public static string GetSourceProviderName(HardwareMetrics metrics, ProviderMetricKind kind)
        {
            if (metrics == null || metrics.Source == null)
            {
                return null;
            }

            MetricSource source = metrics.Source;
            switch (kind)
            {
                case ProviderMetricKind.Fps:
                    return source.FPS;
                case ProviderMetricKind.Fps1PercentLow:
                    return source.FPS1PercentLow;
                case ProviderMetricKind.Fps0Point1PercentLow:
                    return source.FPS0Point1PercentLow;
                case ProviderMetricKind.CpuUsage:
                    return source.CpuUsage;
                case ProviderMetricKind.CpuTemperature:
                    return source.CpuTemperature;
                case ProviderMetricKind.CpuPower:
                    return source.CpuPower;
                case ProviderMetricKind.GpuUsage:
                    return source.GpuUsage;
                case ProviderMetricKind.GpuTemperature:
                    return source.GpuTemperature;
                case ProviderMetricKind.GpuPower:
                    return source.GpuPower;
                case ProviderMetricKind.RamUsage:
                    return source.RamUsage;
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// One row in the global legend: provider name, color swatch, and visibility for all charts.
    /// </summary>
    public sealed class ProviderVisibilityItem : ObservableObject
    {
        private readonly Action _onSelectionChanged;
        private bool _isSelected;

        public ProviderVisibilityItem(string providerName, Brush legendBrush, Action onSelectionChanged)
        {
            ProviderName = providerName ?? string.Empty;
            LegendBrush = legendBrush;
            _onSelectionChanged = onSelectionChanged;
        }

        public string ProviderName { get; }

        public Brush LegendBrush { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                SetValue(ref _isSelected, value);
                _onSelectionChanged?.Invoke();
            }
        }

        /// <summary>
        /// Updates the checkbox without notifying the parent (bulk updates when enabling aggregation mode).
        /// </summary>
        public void SetSelectedSilently(bool value)
        {
            if (_isSelected == value)
            {
                return;
            }

            SetValue(ref _isSelected, value);
        }
    }

    /// <summary>
    /// Drives live line charts comparing the same metric across hardware monitoring providers.
    /// Samples are taken every five seconds on a background thread; UI updates are marshalled
    /// to the Playnite dispatcher. Either follow aggregation source per metric, or pick raw providers.
    /// </summary>
    public sealed class ProviderPerformanceChartsViewModel : ObservableObject, IDisposable
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private const int MaxPointsPerSeries = 72;

        private readonly GameActivityMonitoring _monitoring;
        private readonly DispatcherTimer _timer;
        private readonly List<string> _providerNames;
        private readonly List<ProviderPerfMetricChartVm> _chartModels;

        private string _headerSubtext;
        private bool _disposed;
        private bool _suppressLegendCallbacks;
        private bool _useAggregatorSourcesOnly = true;

        /// <summary>
        /// Explains refresh cadence, or why charts are empty (no providers).
        /// </summary>
        public string HeaderSubtext
        {
            get => _headerSubtext;
            private set => SetValue(ref _headerSubtext, value);
        }

        /// <summary>
        /// Title above the global provider legend.
        /// </summary>
        public string LegendSectionTitle { get; private set; }

        /// <summary>
        /// True when at least one provider is registered (shows the global legend strip).
        /// </summary>
        public bool HasProviderLegend { get; private set; }

        /// <summary>
        /// Global legend: one entry per provider with visibility toggle for all charts.
        /// </summary>
        public ObservableCollection<ProviderVisibilityItem> ProviderLegendItems { get; }

        /// <summary>
        /// When true, each chart shows only the provider that supplies that metric in the aggregated snapshot.
        /// When false, visibility follows the per-provider checkboxes (raw comparison).
        /// </summary>
        public bool UseAggregatorSourcesOnly
        {
            get => _useAggregatorSourcesOnly;
            set
            {
                if (_useAggregatorSourcesOnly == value)
                {
                    return;
                }

                SetValue(ref _useAggregatorSourcesOnly, value);
                if (value)
                {
                    _suppressLegendCallbacks = true;
                    foreach (ProviderVisibilityItem item in ProviderLegendItems)
                    {
                        item.SetSelectedSilently(false);
                    }

                    _suppressLegendCallbacks = false;
                }

                RefreshChartVisibility();
            }
        }

        /// <summary>Label for the aggregation-mode checkbox (right of provider list).</summary>
        public string AggregatorModeCaption { get; private set; }

        /// <summary>
        /// One LiveCharts panel per metric (FPS, CPU usage, etc.).
        /// </summary>
        public ObservableCollection<ProviderPerfMetricChartVm> Charts { get; }

        public ProviderPerformanceChartsViewModel(GameActivityMonitoring monitoring)
        {
            _monitoring = monitoring ?? throw new ArgumentNullException(nameof(monitoring));
            Charts = new ObservableCollection<ProviderPerfMetricChartVm>();
            ProviderLegendItems = new ObservableCollection<ProviderVisibilityItem>();
            _chartModels = new List<ProviderPerfMetricChartVm>();
            _providerNames = monitoring.GetRegisteredMonitoringProviderNames().ToList();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += OnTimerTick;

            LegendSectionTitle = ResourceProvider.GetString("LOCGameActivityProviderPerfChartsLegend")
                ?? "By provider";
            AggregatorModeCaption = ResourceProvider.GetString("LOCGameActivityProviderPerfChartsAggregatorMode")
                ?? "Used values (aggregation)";

            if (_providerNames.Count == 0)
            {
                HasProviderLegend = false;
                HeaderSubtext = ResourceProvider.GetString("LOCGameActivityProviderPerfChartsNoProviders")
                    ?? "No hardware monitoring providers are registered.";
                return;
            }

            HasProviderLegend = true;
            BuildLegendItems();
            BuildCharts();
            RefreshChartVisibility();
            HeaderSubtext = ResourceProvider.GetString("LOCGameActivityProviderPerfChartsHint")
                ?? string.Empty;
        }

        /// <summary>
        /// Starts periodic sampling (first sample runs immediately, then every five seconds).
        /// </summary>
        public void Start()
        {
            if (_disposed || _providerNames.Count == 0)
            {
                return;
            }

            OnTimerTick(null, EventArgs.Empty);
            _timer.Start();
        }

        private void BuildLegendItems()
        {
            for (int i = 0; i < _providerNames.Count; i++)
            {
                string name = _providerNames[i];
                Brush brush = ProviderChartColors.CreateBrush(i);
                ProviderLegendItems.Add(new ProviderVisibilityItem(name, brush, OnLegendSelectionChanged));
            }
        }

        private void BuildCharts()
        {
            foreach (MetricChartDefinition def in MetricChartDefinition.All)
            {
                string title = ResourceProvider.GetString(def.TitleResourceKey) ?? def.TitleResourceKey;
                var chartVm = new ProviderPerfMetricChartVm(title, def.Kind, _providerNames);
                _chartModels.Add(chartVm);
                Charts.Add(chartVm);
            }
        }

        private void OnLegendSelectionChanged()
        {
            if (_suppressLegendCallbacks)
            {
                return;
            }

            if (ProviderLegendItems.Any(x => x.IsSelected))
            {
                if (_useAggregatorSourcesOnly)
                {
                    _useAggregatorSourcesOnly = false;
                    OnPropertyChanged(nameof(UseAggregatorSourcesOnly));
                }
            }

            RefreshChartVisibility();
        }

        private void RefreshChartVisibility()
        {
            HardwareMetrics aggregated = null;
            if (_useAggregatorSourcesOnly)
            {
                aggregated = _monitoring.GetAggregatedMetricsSnapshot();
            }

            bool[] providerSelection = ProviderLegendItems.Select(x => x.IsSelected).ToArray();
            foreach (ProviderPerfMetricChartVm chart in _chartModels)
            {
                chart.ApplyVisibility(_useAggregatorSourcesOnly, aggregated, _providerNames, providerSelection);
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (_disposed || _providerNames.Count == 0)
            {
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    Dictionary<string, HardwareMetrics> snapshot = _monitoring.GetProviderRawMetricsSnapshot();
                    string label = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture);

                    if (API.Instance?.MainView?.UIDispatcher == null)
                    {
                        return;
                    }

                    API.Instance.MainView.UIDispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_disposed)
                        {
                            return;
                        }

                        foreach (ProviderPerfMetricChartVm chart in _chartModels)
                        {
                            chart.AppendSample(label, snapshot, _providerNames, MaxPointsPerSeries);
                        }

                        RefreshChartVisibility();
                    }));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Provider performance charts sampling failed");
                }
            });
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
        }

        private sealed class MetricChartDefinition
        {
            public MetricChartDefinition(string titleResourceKey, ProviderMetricKind kind)
            {
                TitleResourceKey = titleResourceKey;
                Kind = kind;
            }

            public string TitleResourceKey { get; }
            public ProviderMetricKind Kind { get; }

            public static readonly MetricChartDefinition[] All =
            {
                new MetricChartDefinition("LOCGameActivityFps", ProviderMetricKind.Fps),
                new MetricChartDefinition("LOCGameActivityFps1PercentLow", ProviderMetricKind.Fps1PercentLow),
                new MetricChartDefinition("LOCGameActivityFps0Point1PercentLow", ProviderMetricKind.Fps0Point1PercentLow),
                new MetricChartDefinition("LOCGameActivityCpuUsage", ProviderMetricKind.CpuUsage),
                new MetricChartDefinition("LOCGameActivityCpuTemp", ProviderMetricKind.CpuTemperature),
                new MetricChartDefinition("LOCGameActivityCpuPower", ProviderMetricKind.CpuPower),
                new MetricChartDefinition("LOCGameActivityGpuUsage", ProviderMetricKind.GpuUsage),
                new MetricChartDefinition("LOCGameActivityGpuTemp", ProviderMetricKind.GpuTemperature),
                new MetricChartDefinition("LOCGameActivityGpuPower", ProviderMetricKind.GpuPower),
                new MetricChartDefinition("LOCGameActivityRamUsage", ProviderMetricKind.RamUsage)
            };
        }
    }

    /// <summary>
    /// Metric identity for provider chart extraction.
    /// </summary>
    public enum ProviderMetricKind
    {
        Fps,
        Fps1PercentLow,
        Fps0Point1PercentLow,
        CpuUsage,
        CpuTemperature,
        CpuPower,
        GpuUsage,
        GpuTemperature,
        GpuPower,
        RamUsage
    }

    /// <summary>
    /// One Cartesian chart: one line series per provider for a single metric.
    /// Y-axis bounds follow only the series that are currently visible (global legend checkboxes).
    /// </summary>
    public sealed class ProviderPerfMetricChartVm : ObservableObject
    {
        private const double VisibleStrokeThickness = 2d;

        private readonly ProviderMetricKind _kind;
        private readonly List<LineSeries> _seriesList;
        private readonly List<Brush> _originalStrokes;
        private readonly List<bool> _seriesVisible;

        private double _axisYMin;
        private double _axisYMax;

        /// <param name="title">Localized chart title.</param>
        /// <param name="kind">Which field to read from <see cref="HardwareMetrics"/>.</param>
        /// <param name="providerNames">Stable provider ordering shared across charts.</param>
        public ProviderPerfMetricChartVm(string title, ProviderMetricKind kind, IList<string> providerNames)
        {
            Title = title ?? string.Empty;
            _kind = kind;
            Labels = new ObservableCollection<string>();
            Series = new SeriesCollection();
            _seriesList = new List<LineSeries>();
            _originalStrokes = new List<Brush>();
            _seriesVisible = new List<bool>();

            int colorIndex = 0;
            foreach (string name in providerNames)
            {
                Brush stroke = ProviderChartColors.CreateBrush(colorIndex++);
                _originalStrokes.Add(stroke);
                _seriesVisible.Add(true);

                var line = new LineSeries
                {
                    Title = name,
                    Values = new ChartValues<double>(),
                    LineSmoothness = 0,
                    PointGeometrySize = 0,
                    StrokeThickness = VisibleStrokeThickness,
                    Fill = Brushes.Transparent,
                    Stroke = stroke
                };

                _seriesList.Add(line);
                Series.Add(line);
            }

            _axisYMin = 0d;
            _axisYMax = 100d;
        }

        /// <summary>Localized metric name.</summary>
        public string Title { get; }

        public SeriesCollection Series { get; }

        public ObservableCollection<string> Labels { get; }

        /// <summary>Lower bound of the Y axis (visible series only).</summary>
        public double AxisYMin
        {
            get => _axisYMin;
            private set => SetValue(ref _axisYMin, value);
        }

        /// <summary>Upper bound of the Y axis (visible series only).</summary>
        public double AxisYMax
        {
            get => _axisYMax;
            private set => SetValue(ref _axisYMax, value);
        }

        /// <summary>
        /// Applies visibility either from aggregation winners (one provider per metric) or from provider checkboxes.
        /// </summary>
        public void ApplyVisibility(
            bool useAggregatorSourcesOnly,
            HardwareMetrics aggregated,
            IList<string> providerNames,
            IReadOnlyList<bool> providerSelected)
        {
            for (int i = 0; i < _seriesList.Count; i++)
            {
                bool visible;
                if (useAggregatorSourcesOnly)
                {
                    string winner = AggregatedMetricSourceResolver.GetSourceProviderName(aggregated, _kind);
                    visible = !string.IsNullOrEmpty(winner)
                        && i < providerNames.Count
                        && string.Equals(winner, providerNames[i], StringComparison.Ordinal);
                }
                else
                {
                    visible = i < providerSelected.Count && providerSelected[i];
                }

                ApplySeriesStroke(i, visible);
            }

            RecalculateYAxisFromVisibleSeries();
        }

        private void ApplySeriesStroke(int seriesIndex, bool visible)
        {
            if (seriesIndex < 0 || seriesIndex >= _seriesList.Count)
            {
                return;
            }

            _seriesVisible[seriesIndex] = visible;
            LineSeries line = _seriesList[seriesIndex];
            if (visible)
            {
                line.Stroke = _originalStrokes[seriesIndex];
                line.StrokeThickness = VisibleStrokeThickness;
            }
            else
            {
                line.Stroke = Brushes.Transparent;
                line.StrokeThickness = 0d;
            }
        }

        /// <summary>
        /// Appends one shared time label and one value per provider line.
        /// </summary>
        public void AppendSample(
            string timeLabel,
            Dictionary<string, HardwareMetrics> snapshot,
            IList<string> providerNames,
            int maxPoints)
        {
            Labels.Add(timeLabel);

            for (int i = 0; i < providerNames.Count; i++)
            {
                string providerName = providerNames[i];
                LineSeries line = _seriesList[i];
                HardwareMetrics metrics = null;
                if (snapshot != null)
                {
                    snapshot.TryGetValue(providerName, out metrics);
                }

                double value = ReadMetric(metrics);
                ((ChartValues<double>)line.Values).Add(value);
            }

            TrimOldPoints(maxPoints);
        }

        /// <summary>
        /// Sets Y-axis min/max from values of visible series only (ignores hidden providers).
        /// </summary>
        private void RecalculateYAxisFromVisibleSeries()
        {
            bool anyVisibleSeries = false;
            bool anyDataPoint = false;
            double minV = double.MaxValue;
            double maxV = double.MinValue;

            for (int i = 0; i < _seriesList.Count; i++)
            {
                if (!_seriesVisible[i])
                {
                    continue;
                }

                anyVisibleSeries = true;
                var values = (ChartValues<double>)_seriesList[i].Values;
                foreach (double v in values)
                {
                    anyDataPoint = true;
                    if (v < minV)
                    {
                        minV = v;
                    }

                    if (v > maxV)
                    {
                        maxV = v;
                    }
                }
            }

            if (!anyVisibleSeries || !anyDataPoint)
            {
                AxisYMin = 0d;
                AxisYMax = 1d;
                return;
            }

            if (minV == maxV)
            {
                double c = minV;
                minV = c - 1d;
                maxV = c + 1d;
            }

            double range = maxV - minV;
            double pad = Math.Max(range * 0.08d, 0.5d);
            double newMin = minV - pad;
            double newMax = maxV + pad;

            if (minV >= 0d && maxV >= 0d)
            {
                newMin = Math.Max(0d, newMin);
            }

            AxisYMin = newMin;
            AxisYMax = newMax;
        }

        private double ReadMetric(HardwareMetrics metrics)
        {
            if (metrics == null)
            {
                return 0d;
            }

            switch (_kind)
            {
                case ProviderMetricKind.Fps:
                    return ToDouble(metrics.FPS);
                case ProviderMetricKind.Fps1PercentLow:
                    return ToDouble(metrics.FPS1PercentLow);
                case ProviderMetricKind.Fps0Point1PercentLow:
                    return ToDouble(metrics.FPS0Point1PercentLow);
                case ProviderMetricKind.CpuUsage:
                    return ToDouble(metrics.CpuUsage);
                case ProviderMetricKind.CpuTemperature:
                    return ToDouble(metrics.CpuTemperature);
                case ProviderMetricKind.CpuPower:
                    return ToDouble(metrics.CpuPower);
                case ProviderMetricKind.GpuUsage:
                    return ToDouble(metrics.GpuUsage);
                case ProviderMetricKind.GpuTemperature:
                    return ToDouble(metrics.GpuTemperature);
                case ProviderMetricKind.GpuPower:
                    return ToDouble(metrics.GpuPower);
                case ProviderMetricKind.RamUsage:
                    return ToDouble(metrics.RamUsage);
                default:
                    return 0d;
            }
        }

        private static double ToDouble(int? value)
        {
            return value.HasValue ? value.Value : 0d;
        }

        private void TrimOldPoints(int maxPoints)
        {
            while (Labels.Count > maxPoints)
            {
                Labels.RemoveAt(0);
                foreach (LineSeries line in _seriesList)
                {
                    var values = (ChartValues<double>)line.Values;
                    if (values.Count > 0)
                    {
                        values.RemoveAt(0);
                    }
                }
            }
        }
    }
}
