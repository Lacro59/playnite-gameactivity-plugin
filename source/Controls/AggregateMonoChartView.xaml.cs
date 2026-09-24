using System.Windows;
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
    /// Aggregate chart host (Games / Genres / Tags): columns plus optional complementary pie (Games / Genres only).
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

        /// <summary>Complementary pie chart (Games / Genres).</summary>
        public PieChart ChartPie => PART_ChartPie;

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

        /// <summary>Pie tooltip instance from XAML.</summary>
        public CustomerToolTipForTime ChartPieToolTip => PART_ChartPie_ToolTip;

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
            bool showPie;

            switch (kind)
            {
                case AggregateKind.Games:
                    titleKey = "LOCGameActivityTotalHoursByGames";
                    showIcon = true;
                    showLabel = true;
                    mode = TextBlockWithIconMode.IconFirstWithText;
                    showPie = true;
                    break;
                case AggregateKind.Genres:
                    titleKey = "LOCGameActivityTotalHoursByGenres";
                    showIcon = false;
                    showLabel = true;
                    mode = TextBlockWithIconMode.TextOnly;
                    showPie = true;
                    break;
                case AggregateKind.Tags:
                    titleKey = "LOCGameActivityTotalHoursByTags";
                    showIcon = false;
                    showLabel = true;
                    mode = TextBlockWithIconMode.TextOnly;
                    showPie = false;
                    break;
                default:
                    titleKey = "LOCGameActivityTotalHoursByGames";
                    showIcon = false;
                    showLabel = false;
                    mode = TextBlockWithIconMode.TextOnly;
                    showPie = false;
                    break;
            }

            PART_Chart_Label.Text = ResourceProvider.GetString(titleKey);
            PART_Chart_ToolTip.ShowIcon = showIcon;
            PART_Chart_ToolTip.ShowLabel = showLabel;
            PART_Chart_ToolTip.Mode = mode;
            PART_ChartPie_ToolTip.ShowIcon = showIcon;
            PART_ChartPie_ToolTip.ShowLabel = showLabel;
            PART_ChartPie_ToolTip.Mode = mode;
            PART_ChartPie_ToolTip.ShowSeriesColor = showPie;
            BindChartTooltip();
            BindPieTooltip();
            SetPieVisible(showPie);
            Common.LogDebug($"PeriodView: AggregateMono ApplyKind={kind} tooltip icon={showIcon} label={showLabel} mode={mode} pie={showPie}");
        }

        /// <summary>
        /// Shows or hides the complementary pie column (Tags stays columns-only).
        /// </summary>
        /// <param name="visible">True to show the pie panel.</param>
        public void SetPieVisible(bool visible)
        {
            if (visible)
            {
                PART_PieColumn.Width = new GridLength(0.45, GridUnitType.Star);
                PART_ChartPie.Visibility = Visibility.Visible;
                Grid.SetColumnSpan(PART_Chart_Label, 2);
            }
            else
            {
                PART_PieColumn.Width = new GridLength(0);
                PART_ChartPie.Visibility = Visibility.Collapsed;
                PART_ChartPie.Series = null;
                Grid.SetColumnSpan(PART_Chart_Label, 1);
            }
        }

        /// <summary>
        /// Re-assigns the kind-configured tooltip instance to the column chart.
        /// </summary>
        public void BindChartTooltip()
        {
            PART_Chart.DataTooltip = PART_Chart_ToolTip;
        }

        /// <summary>
        /// Re-assigns the kind-configured tooltip instance to the pie chart.
        /// </summary>
        public void BindPieTooltip()
        {
            PART_ChartPie_ToolTip.ShowSeriesColor = Kind == AggregateKind.Games || Kind == AggregateKind.Genres;
            PART_ChartPie.DataTooltip = PART_ChartPie_ToolTip;
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
