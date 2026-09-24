using System.Collections.Generic;
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
    /// Tooltip matrix (name/date/icon flags) lives only in the Bind*Tooltip methods below.
    /// </summary>
    public partial class AggregateSourcesChartView : UserControl
    {
        private CustomerToolTipForTime _weekCumulToolTip;

        /// <summary>True after the first successful <see cref="MarkLoaded"/> (lazy entry).</summary>
        public bool HasBeenLoaded { get; private set; }

        /// <summary>True when period changed while this view was not active.</summary>
        public bool IsStale { get; private set; }

        /// <summary>Total chart tooltip.</summary>
        public CustomerToolTipForTime ChartTotalToolTip => PART_ChartTotal_ToolTip;

        /// <summary>Day chart tooltip (date + time).</summary>
        public CustomerToolTipForTime ChartByDayToolTip => PART_ChartByDay_ToolTip;

        /// <summary>Week chart multi-source tooltip (XAML default; swapped for cumul).</summary>
        public CustomerToolTipForMultipleTime ChartByWeekToolTip => PART_ChartByWeek_ToolTip;

        /// <summary>Total hours by source chart.</summary>
        public CartesianChart ChartTotal => PART_ChartTotal;

        /// <summary>Complementary pie for total hours by source.</summary>
        public PieChart ChartTotalPie => PART_ChartTotalPie;

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
        /// Applies the Sources tooltip matrix once (total / day / week multi defaults).
        /// Call at host creation; reloads re-apply via Bind*Tooltip.
        /// </summary>
        /// <param name="showIcon">Whether launcher icons are enabled in settings.</param>
        /// <param name="modeComplet">Icon+text mode from settings (week multi).</param>
        public void ConfigureDefaultTooltips(bool showIcon, TextBlockWithIconMode modeComplet)
        {
            BindTotalTooltip();
            BindTotalPieTooltip();
            BindDayTooltip();
            BindWeekSourcesTooltip(showIcon, modeComplet, null);
            Common.LogDebug($"PeriodView: AggregateSources ConfigureDefaultTooltips matrix applied showIcon={showIcon}");
        }

        /// <summary>
        /// Total chart: source name + playtime (no icon).
        /// </summary>
        public void BindTotalTooltip()
        {
            PART_ChartTotal_ToolTip.ShowIcon = false;
            PART_ChartTotal_ToolTip.ShowLabel = true;
            PART_ChartTotal_ToolTip.Mode = TextBlockWithIconMode.TextOnly;
            PART_ChartTotal.DataTooltip = PART_ChartTotal_ToolTip;
        }

        /// <summary>
        /// Total pie: source name + playtime (no icon), same matrix as <see cref="BindTotalTooltip"/>.
        /// </summary>
        public void BindTotalPieTooltip()
        {
            PART_ChartTotalPie_ToolTip.ShowIcon = false;
            PART_ChartTotalPie_ToolTip.ShowLabel = true;
            PART_ChartTotalPie_ToolTip.ShowSeriesColor = true;
            PART_ChartTotalPie_ToolTip.Mode = TextBlockWithIconMode.TextOnly;
            PART_ChartTotalPie.DataTooltip = PART_ChartTotalPie_ToolTip;
        }

        /// <summary>
        /// Day chart (principal): date + playtime.
        /// </summary>
        public void BindDayTooltip()
        {
            PART_ChartByDay_ToolTip.ShowIcon = false;
            PART_ChartByDay_ToolTip.ShowLabel = true;
            PART_ChartByDay_ToolTip.Mode = TextBlockWithIconMode.TextOnly;
            PART_ChartByDay.DataTooltip = PART_ChartByDay_ToolTip;
        }

        /// <summary>
        /// Week chart cumul: playtime only (+ week range in title when DatesPeriodes set).
        /// </summary>
        /// <param name="datesPeriodes">Week date ranges for the title subtitle; may be null.</param>
        public void BindWeekCumulTooltip(List<WeekStartEnd> datesPeriodes)
        {
            if (_weekCumulToolTip == null)
            {
                _weekCumulToolTip = new CustomerToolTipForTime();
            }

            _weekCumulToolTip.ShowIcon = false;
            _weekCumulToolTip.ShowLabel = false;
            _weekCumulToolTip.ShowTitle = true;
            _weekCumulToolTip.Mode = TextBlockWithIconMode.TextOnly;
            _weekCumulToolTip.ShowWeekPeriode = true;
            _weekCumulToolTip.DatesPeriodes = datesPeriodes ?? new List<WeekStartEnd>();
            PART_ChartByWeek.DataTooltip = _weekCumulToolTip;
        }

        /// <summary>
        /// Week chart by source: icon + name + playtime (multi-series).
        /// </summary>
        /// <param name="showIcon">Whether launcher icons are enabled.</param>
        /// <param name="modeComplet">Icon+text mode from settings.</param>
        /// <param name="datesPeriodes">Week date ranges for the title subtitle; may be null.</param>
        public void BindWeekSourcesTooltip(bool showIcon, TextBlockWithIconMode modeComplet, List<WeekStartEnd> datesPeriodes)
        {
            PART_ChartByWeek_ToolTip.ShowIcon = showIcon;
            PART_ChartByWeek_ToolTip.ShowTitle = true;
            PART_ChartByWeek_ToolTip.Mode = modeComplet;
            PART_ChartByWeek_ToolTip.ShowWeekPeriode = true;
            PART_ChartByWeek_ToolTip.DatesPeriodes = datesPeriodes ?? new List<WeekStartEnd>();
            PART_ChartByWeek.DataTooltip = PART_ChartByWeek_ToolTip;
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
