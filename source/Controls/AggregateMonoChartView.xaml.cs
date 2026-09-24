using System.Windows.Controls;
using CommonPluginsControls.Controls;
using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsShared;
using GameActivity.Models;
using LiveCharts.Wpf;
using Playnite.SDK;

namespace GameActivity.Controls
{
    /// <summary>
    /// Single-column aggregate chart (Games / Genres / Tags) with fixed tooltip settings per kind.
    /// </summary>
    public partial class AggregateMonoChartView : UserControl
    {
        /// <summary>True after the first successful <see cref="MarkLoaded"/>.</summary>
        public bool HasBeenLoaded { get; private set; }

        /// <summary>True when period changed while this view was not active.</summary>
        public bool IsStale { get; private set; }

        /// <summary>Configured aggregate kind for this instance.</summary>
        public AggregateKind Kind { get; private set; }

        /// <summary>Main column chart.</summary>
        public CartesianChart Chart => PART_Chart;

        /// <summary>X axis.</summary>
        public Axis ChartX => PART_Chart_X;

        /// <summary>Y axis.</summary>
        public Axis ChartY => PART_Chart_Y;

        /// <summary>Title label.</summary>
        public TextBlock ChartLabel => PART_Chart_Label;

        /// <summary>Card border.</summary>
        public Border TotalHoursCard => PART_TotalHoursCard;

        /// <summary>Inner chart grid.</summary>
        public Grid ChartGrid => PART_ChartGrid;

        /// <summary>Default tooltip instance from XAML (replaced on each reload).</summary>
        public CustomerToolTipForTime ChartToolTip => PART_Chart_ToolTip;

        /// <summary>
        /// Initializes the mono aggregate chart view.
        /// </summary>
        public AggregateMonoChartView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Applies fixed title and tooltip flags for the given aggregate kind.
        /// </summary>
        /// <param name="kind">Games, Genres, or Tags.</param>
        public void ApplyKind(AggregateKind kind)
        {
            Kind = kind;
            string titleKey;
            bool showIcon;
            bool showLabel;
            TextBlockWithIconMode mode;

            switch (kind)
            {
                case AggregateKind.Games:
                    titleKey = "LOCGameActivityTotalHoursByGames";
                    showIcon = true;
                    showLabel = true;
                    mode = TextBlockWithIconMode.IconFirstWithText;
                    break;
                case AggregateKind.Genres:
                    titleKey = "LOCGameActivityTotalHoursByGenres";
                    showIcon = false;
                    showLabel = true;
                    mode = TextBlockWithIconMode.TextOnly;
                    break;
                case AggregateKind.Tags:
                    titleKey = "LOCGameActivityTotalHoursByTags";
                    showIcon = false;
                    showLabel = true;
                    mode = TextBlockWithIconMode.TextOnly;
                    break;
                default:
                    titleKey = "LOCGameActivityTotalHoursByGames";
                    showIcon = false;
                    showLabel = false;
                    mode = TextBlockWithIconMode.TextOnly;
                    break;
            }

            PART_Chart_Label.Text = ResourceProvider.GetString(titleKey);
            PART_Chart_ToolTip.ShowIcon = showIcon;
            PART_Chart_ToolTip.ShowLabel = showLabel;
            PART_Chart_ToolTip.Mode = mode;
            BindChartTooltip();
            Common.LogDebug($"PeriodView: AggregateMono ApplyKind={kind} tooltip icon={showIcon} label={showLabel} mode={mode}");
        }

        /// <summary>
        /// Re-assigns the kind-configured tooltip instance to the chart (single source of truth with <see cref="ApplyKind"/>).
        /// </summary>
        public void BindChartTooltip()
        {
            PART_Chart.DataTooltip = PART_Chart_ToolTip;
        }

        /// <summary>
        /// Marks the view as loaded after the first data bind.
        /// </summary>
        public void MarkLoaded()
        {
            HasBeenLoaded = true;
            IsStale = false;
            Common.LogDebug($"PeriodView: AggregateMono MarkLoaded kind={Kind}");
        }

        /// <summary>
        /// Marks charts stale so the next selection reloads them.
        /// </summary>
        public void Invalidate()
        {
            if (HasBeenLoaded)
            {
                IsStale = true;
                Common.LogDebug($"PeriodView: AggregateMono Invalidate kind={Kind}");
            }
        }

        /// <summary>
        /// Whether a reload is required before showing this chart.
        /// </summary>
        public bool NeedsReload
        {
            get { return !HasBeenLoaded || IsStale; }
        }
    }

    /// <summary>Games aggregate mono chart (icon + name + time tooltip).</summary>
    public class AggregateGamesChartView : AggregateMonoChartView
    {
        /// <summary>Initializes a Games aggregate chart.</summary>
        public AggregateGamesChartView()
        {
            ApplyKind(AggregateKind.Games);
        }
    }

    /// <summary>Genres aggregate mono chart (name + time tooltip).</summary>
    public class AggregateGenresChartView : AggregateMonoChartView
    {
        /// <summary>Initializes a Genres aggregate chart.</summary>
        public AggregateGenresChartView()
        {
            ApplyKind(AggregateKind.Genres);
        }
    }

    /// <summary>Tags aggregate mono chart (name + time tooltip).</summary>
    public class AggregateTagsChartView : AggregateMonoChartView
    {
        /// <summary>Initializes a Tags aggregate chart.</summary>
        public AggregateTagsChartView()
        {
            ApplyKind(AggregateKind.Tags);
        }
    }
}
