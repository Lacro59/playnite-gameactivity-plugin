using System.Windows;
using System.Windows.Controls;
using CommonPluginsControls.Controls;
using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsShared;
using LiveCharts.Wpf;

namespace GameActivity.Controls
{
    /// <summary>
    /// Sources aggregate charts: total playtime by store, hours by day, and hours by week.
    /// Owned by <see cref="Views.GameActivityView"/>; period bounds stay in the parent ViewModel.
    /// </summary>
    public partial class AggregateSourcesChartView : UserControl
    {
        /// <summary>True after the first successful <see cref="MarkLoaded"/> (lazy entry).</summary>
        public bool HasBeenLoaded { get; private set; }

        /// <summary>True when period changed while this view was not active.</summary>
        public bool IsStale { get; private set; }

        /// <summary>Total chart tooltip.</summary>
        public CustomerToolTipForTime ChartTotalToolTip => PART_ChartTotal_ToolTip;

        /// <summary>Day chart tooltip.</summary>
        public CustomerToolTipForMultipleTime ChartByDayToolTip => PART_ChartByDay_ToolTip;

        /// <summary>Week chart tooltip.</summary>
        public CustomerToolTipForMultipleTime ChartByWeekToolTip => PART_ChartByWeek_ToolTip;

        /// <summary>Total hours by source chart.</summary>
        public CartesianChart ChartTotal => PART_ChartTotal;

        /// <summary>Total chart X axis.</summary>
        public Axis ChartTotalX => PART_ChartTotal_X;

        /// <summary>Total chart Y axis.</summary>
        public Axis ChartTotalY => PART_ChartTotal_Y;

        /// <summary>Total chart title label.</summary>
        public TextBlock ChartTotalLabel => PART_ChartTotal_Label;

        /// <summary>Total hours card border.</summary>
        public Border TotalHoursCard => PART_TotalHoursCard;

        /// <summary>Inner grid hosting the total chart.</summary>
        public Grid TotalHoursGrid => gridMonth;

        /// <summary>Hours-by-day chart.</summary>
        public CartesianChart ChartByDay => PART_ChartByDay;

        /// <summary>Day chart X axis.</summary>
        public Axis ChartByDayX => PART_ChartByDay_X;

        /// <summary>Day chart Y axis.</summary>
        public Axis ChartByDayY => PART_ChartByDay_Y;

        /// <summary>Day chart title.</summary>
        public TextBlock DayLabel => PART_DayLabel;

        /// <summary>Day chart card.</summary>
        public Border HoursByDayCard => PART_HoursByDayCard;

        /// <summary>Day chart host grid (legacy layout tweaks).</summary>
        public Grid DayGrid => GridDay;

        /// <summary>Hours-by-week chart.</summary>
        public CartesianChart ChartByWeek => PART_ChartByWeek;

        /// <summary>Week chart X axis.</summary>
        public Axis ChartByWeekX => PART_ChartByWeek_X;

        /// <summary>Week chart Y axis.</summary>
        public Axis ChartByWeekY => PART_ChartByWeek_Y;

        /// <summary>Week chart title.</summary>
        public TextBlock WeekLabel => PART_WeekLabel;

        /// <summary>Week chart card.</summary>
        public Border HoursByWeekCard => PART_HoursByWeekCard;

        /// <summary>
        /// Initializes the Sources aggregate chart view.
        /// </summary>
        public AggregateSourcesChartView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Applies launcher icon / tooltip defaults for Sources (time-only on total; multi-time day/week).
        /// </summary>
        /// <param name="showIcon">Whether launcher icons are enabled in settings.</param>
        /// <param name="modeComplet">Icon+text mode from settings (kept for day/week multi tooltips).</param>
        public void ConfigureDefaultTooltips(bool showIcon, TextBlockWithIconMode modeComplet)
        {
            PART_ChartTotal_ToolTip.ShowIcon = false;
            PART_ChartTotal_ToolTip.ShowLabel = false;
            PART_ChartTotal_ToolTip.Mode = TextBlockWithIconMode.TextOnly;

            PART_ChartByDay_ToolTip.ShowIcon = showIcon;
            PART_ChartByDay_ToolTip.Mode = modeComplet;

            PART_ChartByWeek_ToolTip.ShowIcon = showIcon;
            PART_ChartByWeek_ToolTip.Mode = modeComplet;
            PART_ChartByWeek_ToolTip.ShowWeekPeriode = true;
            Common.LogDebug($"PeriodView: AggregateSources ConfigureDefaultTooltips total=timeOnly day/week showIcon={showIcon}");
        }

        /// <summary>
        /// Marks the view as loaded after the first data bind (lazy gate).
        /// </summary>
        public void MarkLoaded()
        {
            HasBeenLoaded = true;
            IsStale = false;
            Common.LogDebug("PeriodView: AggregateSources MarkLoaded");
        }

        /// <summary>
        /// Marks charts stale so the next Sources selection reloads them.
        /// </summary>
        public void Invalidate()
        {
            if (HasBeenLoaded)
            {
                IsStale = true;
                Common.LogDebug("PeriodView: AggregateSources Invalidate");
            }
        }

        /// <summary>
        /// Whether a reload is required before showing Sources charts.
        /// </summary>
        public bool NeedsReload
        {
            get { return !HasBeenLoaded || IsStale; }
        }
    }
}
