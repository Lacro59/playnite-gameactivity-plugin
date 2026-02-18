using CommonPlayniteShared.Common;
using CommonPluginsControls.Controls;
using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Utilities;
using GameActivity.Models;
using GameActivity.Services;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Events;
using LiveCharts.Wpf;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace GameActivity.Controls
{
    public partial class PluginChartTime : PluginUserControlExtend
    {
        private ActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginChartTimeDataContext ControlDataContext = new PluginChartTimeDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginChartTimeDataContext)value; 
        }

        public event DataClickHandler GameSeriesDataClick;


        #region Properties
        public bool DisableAnimations
        {
            get => (bool)GetValue(DisableAnimationsProperty);
            set => SetValue(DisableAnimationsProperty, value);
        }
        public static readonly DependencyProperty DisableAnimationsProperty = DependencyProperty.Register(
            nameof(DisableAnimations), typeof(bool), typeof(PluginChartTime),
            new FrameworkPropertyMetadata(true, ControlsPropertyChangedCallback));

        public bool ShowByWeeks
        {
            get => (bool)GetValue(ShowByWeeksProperty);
            set => SetValue(ShowByWeeksProperty, value);
        }
        public static readonly DependencyProperty ShowByWeeksProperty = DependencyProperty.Register(
            nameof(ShowByWeeks), typeof(bool), typeof(PluginChartTime),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        public bool Truncate
        {
            get => (bool)GetValue(TruncateProperty);
            set => SetValue(TruncateProperty, value);
        }
        public static readonly DependencyProperty TruncateProperty = DependencyProperty.Register(
            nameof(Truncate), typeof(bool), typeof(PluginChartTime),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        public bool LabelsRotation
        {
            get => (bool)GetValue(LabelsRotationProperty);
            set => SetValue(LabelsRotationProperty, value);
        }
        public static readonly DependencyProperty LabelsRotationProperty = DependencyProperty.Register(
            nameof(LabelsRotation), typeof(bool), typeof(PluginChartTime),
            new FrameworkPropertyMetadata(false, ControlsPropertyChangedCallback));

        public static readonly DependencyProperty AxisLimitProperty;
        public int AxisLimit { get; set; }

        public int AxisVariator
        {
            get => (int)GetValue(AxisVariatoryProperty);
            set => SetValue(AxisVariatoryProperty, value);
        }
        public static readonly DependencyProperty AxisVariatoryProperty = DependencyProperty.Register(
            nameof(AxisVariator), typeof(int), typeof(PluginChartTime),
            new FrameworkPropertyMetadata(0, ControlsPropertyChangedCallback));
        #endregion


        public PluginChartTime()
        {
            AlwaysShow = true;
            InitializeComponent();
            DataContext = ControlDataContext;
            Loaded += OnLoaded;
        }

        protected override void AttachStaticEvents()
        {
            base.AttachStaticEvents();

            AttachPluginEvents(PluginDatabase.PluginName, () =>
            {
                PluginDatabase.PluginSettings.PropertyChanged += CreatePluginSettingsHandler();
                PluginDatabase.Database.ItemUpdated += CreateDatabaseItemUpdatedHandler<GameActivities>();
                PluginDatabase.Database.ItemCollectionChanged += CreateDatabaseCollectionChangedHandler<GameActivities>();
                API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;
            });
        }

        protected override void PluginSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Truncate = PluginDatabase.PluginSettings.Settings.ChartTimeTruncate;
            GameContextChanged(null, GameContext);
        }


        public override void SetDefaultDataContext()
        {
            bool isActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationChartTime;
            double chartTimeHeight = PluginDatabase.PluginSettings.Settings.ChartTimeHeight;
            bool chartTimeAxis = PluginDatabase.PluginSettings.Settings.ChartTimeAxis;
            bool chartTimeOrdinates = PluginDatabase.PluginSettings.Settings.ChartTimeOrdinates;

            if (IgnoreSettings)
            {
                isActivated = true;
                chartTimeHeight = double.NaN;
                chartTimeAxis = true;
                chartTimeOrdinates = true;
            }

            ControlDataContext.IsActivated = isActivated;
            ControlDataContext.ChartTimeHeight = chartTimeHeight;
            ControlDataContext.ChartTimeAxis = chartTimeAxis;
            ControlDataContext.ChartTimeOrdinates = chartTimeOrdinates;
            ControlDataContext.ChartTimeVisibleEmpty = PluginDatabase.PluginSettings.Settings.ChartTimeVisibleEmpty;
            ControlDataContext.DisableAnimations = DisableAnimations;
            ControlDataContext.LabelsRotationValue = LabelsRotation ? 160d : 0d;

            PART_ChartTimeActivity.Series = null;
            PART_ChartTimeActivityLabelsX.Labels = null;
        }


        /// <summary>
        /// Resolves the display limit once, then delegates to the appropriate chart builder.
        /// </summary>
        public override void SetData(Game newContext, PluginDataBaseGameBase pluginGameData)
        {
            GameActivities gameActivities = (GameActivities)pluginGameData;

            MustDisplay = !IgnoreSettings && !ControlDataContext.ChartTimeVisibleEmpty
                ? gameActivities.HasData
                : true;

            if (!MustDisplay)
            {
                return;
            }

            int limit = AxisLimit != 0
                ? AxisLimit
                : PluginDatabase.PluginSettings.Settings.ChartTimeCountAbscissa;

            int axisVariator = AxisVariator;

            if (ShowByWeeks)
            {
                GetActivityForGamesChartByWeek(gameActivities, axisVariator, limit);
            }
            else
            {
                GetActivityForGamesTimeGraphics(gameActivities, axisVariator, limit);
            }
        }


        #region Public methods
        public void Next(int value = 1) { AxisVariator += value; }
        public void Prev(int value = 1) { AxisVariator -= value; }
        #endregion


        private void GetActivityForGamesTimeGraphics(GameActivities gameActivities, int variateurTime = 0, int limit = 9)
        {
            try
            {
                if (gameActivities?.FilterItems == null)
                {
                    return;
                }

                string[] listDate = new string[limit + 1];
                ChartValues<CustomerForTime> series1 = new ChartValues<CustomerForTime>();
                ChartValues<CustomerForTime> series2 = new ChartValues<CustomerForTime>();
                ChartValues<CustomerForTime> series3 = new ChartValues<CustomerForTime>();
                ChartValues<CustomerForTime> series4 = new ChartValues<CustomerForTime>();
                ChartValues<CustomerForTime> series5 = new ChartValues<CustomerForTime>();

                bool hasData2 = false;
                bool hasData3 = false;
                bool hasData4 = false;
                bool hasData5 = false;

                List<Activity> activities = Serialization.GetClone(gameActivities.FilterItems);

                if (Truncate)
                {
                    activities = activities.OrderBy(x => x.DateSession).ToList();
                    List<string> dtList = activities
                        .Where(x => x.DateSession != null)
                        .Select(x => ((DateTime)x.DateSession).ToLocalTime().ToString("yyyy-MM-dd"))
                        .Distinct()
                        .ToList();

                    if (dtList.Count > limit && variateurTime != 0)
                    {
                        if (variateurTime > 0)
                        {
                            AxisVariator = 0;
                        }
                        else if (dtList.Count + variateurTime - limit <= 0)
                        {
                            AxisVariator++;
                            for (int idx = dtList.Count - 1; idx > limit; idx--)
                            {
                                if (dtList.Count > limit)
                                {
                                    dtList.RemoveAt(idx);
                                }
                            }
                        }
                        else
                        {
                            int min = dtList.Count + variateurTime - 1;
                            for (int idx = dtList.Count - 1; idx > min; idx--)
                            {
                                if (dtList.Count > limit)
                                {
                                    dtList.RemoveAt(idx);
                                }
                            }
                        }
                    }

                    int countActivities = dtList.Count;
                    int newLimit = (countActivities - limit - 1) >= 0 ? countActivities - limit - 1 : 0;
                    listDate = new string[countActivities - newLimit];

                    for (int i = newLimit; i < countActivities; i++)
                    {
                        string dt = dtList[i];
                        listDate[i - newLimit] = dt;

                        CustomerForTime placeholder = new CustomerForTime { Name = dt, Values = 0, HideIsZero = true };
                        series1.Add(placeholder);
                        series2.Add(placeholder);
                        series3.Add(placeholder);
                        series4.Add(placeholder);
                        series5.Add(placeholder);
                    }
                }
                else
                {
                    DateTime dateStart = new DateTime(1982, 12, 15, 0, 0, 0);
                    foreach (Activity activity in activities)
                    {
                        DateTime dateSession = Convert.ToDateTime(activity.DateSession?.ToLocalTime());
                        if (dateSession > dateStart)
                        {
                            dateStart = dateSession;
                        }
                    }
                    dateStart = dateStart.AddDays(variateurTime);

                    for (int i = limit; i >= 0; i--)
                    {
                        string dateStr = dateStart.AddDays(-i).ToString("yyyy-MM-dd");
                        listDate[limit - i] = dateStr;

                        CustomerForTime placeholder = new CustomerForTime { Name = dateStr, Values = 0, HideIsZero = true };
                        series1.Add(placeholder);
                        series2.Add(placeholder);
                        series3.Add(placeholder);
                        series4.Add(placeholder);
                        series5.Add(placeholder);
                    }
                }

                LocalDateConverter localDateConverter = new LocalDateConverter();
                bool cumulSessions = PluginDatabase.PluginSettings.Settings.CumulPlaytimeSession;

                int effectiveLimit = limit == (listDate.Length - 1) ? limit : (listDate.Length - 1);

                for (int iActivity = 0; iActivity < activities.Count; iActivity++)
                {
                    ulong elapsedSeconds = activities[iActivity].ElapsedSeconds;
                    string dateSession = Convert.ToDateTime(activities[iActivity].DateSession).ToLocalTime().ToString("yyyy-MM-dd");

                    for (int iDay = effectiveLimit; iDay >= 0; iDay--)
                    {
                        if (listDate[iDay] != dateSession)
                        {
                            continue;
                        }

                        string displayName = series1[iDay].Name;
                        try
                        {
                            displayName = (string)localDateConverter.Convert(
                                DateTime.ParseExact(series1[iDay].Name, "yyyy-MM-dd", null).ToLocalTime(),
                                null, null, CultureInfo.CurrentCulture);
                        }
                        catch { }

                        if (cumulSessions)
                        {
                            series1[iDay] = new CustomerForTime { Name = displayName, Values = series1[iDay].Values + (long)elapsedSeconds };
                            continue;
                        }

                        if (series1[iDay].Values == 0) { series1[iDay] = new CustomerForTime { Name = displayName, Values = (long)elapsedSeconds }; continue; }
                        if (series2[iDay].Values == 0) { hasData2 = true; series2[iDay] = new CustomerForTime { Name = displayName, Values = (long)elapsedSeconds }; continue; }
                        if (series3[iDay].Values == 0) { hasData3 = true; series3[iDay] = new CustomerForTime { Name = displayName, Values = (long)elapsedSeconds }; continue; }
                        if (series4[iDay].Values == 0) { hasData4 = true; series4[iDay] = new CustomerForTime { Name = displayName, Values = (long)elapsedSeconds }; continue; }
                        if (series5[iDay].Values == 0) { hasData5 = true; series5[iDay] = new CustomerForTime { Name = displayName, Values = (long)elapsedSeconds }; continue; }
                    }
                }

                SeriesCollection activityForGameSeries = new SeriesCollection();
                if (cumulSessions)
                {
                    activityForGameSeries.Add(new ColumnSeries { Title = "1", Values = series1, Fill = PluginDatabase.PluginSettings.Settings.ChartColors });
                }
                else
                {
                    activityForGameSeries.Add(new ColumnSeries { Title = "1", Values = series1 });
                }

                if (hasData2) { activityForGameSeries.Add(new ColumnSeries { Title = "2", Values = series2 }); }
                if (hasData3) { activityForGameSeries.Add(new ColumnSeries { Title = "3", Values = series3 }); }
                if (hasData4) { activityForGameSeries.Add(new ColumnSeries { Title = "4", Values = series4 }); }
                if (hasData5) { activityForGameSeries.Add(new ColumnSeries { Title = "5", Values = series5 }); }

                for (int iDay = 0; iDay < listDate.Length; iDay++)
                {
                    listDate[iDay] = Convert.ToDateTime(listDate[iDay]).ToString(Constants.DateUiFormat);
                }

                CartesianMapper<CustomerForTime> customerVmMapper = Mappers.Xy<CustomerForTime>()
                    .X((value, index) => index)
                    .Y(value => value.Values);
                Charting.For<CustomerForTime>(customerVmMapper);

                PlayTimeToStringConverterWithZero converter = new PlayTimeToStringConverterWithZero();
                PART_ChartTimeActivityLabelsY.LabelFormatter = value => (string)converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);

                PART_ChartTimeActivity.DataTooltip = cumulSessions
                    ? (System.Windows.Controls.UserControl)new CustomerToolTipForTime
                    {
                        ShowIcon = PluginDatabase.PluginSettings.Settings.ShowLauncherIcons,
                        Mode = PluginDatabase.PluginSettings.Settings.ModeStoreIcon == 1
                            ? TextBlockWithIconMode.IconTextFirstWithText
                            : TextBlockWithIconMode.IconFirstWithText
                    }
                    : new CustomerToolTipForMultipleTime { ShowTitle = false };

                PART_ChartTimeActivityLabelsY.MinValue = 0;
                PART_ChartTimeActivity.Series = activityForGameSeries;
                PART_ChartTimeActivityLabelsX.Labels = listDate;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        private void GetActivityForGamesChartByWeek(GameActivities gameActivities, int variateurTime = 0, int limit = 9)
        {
            try
            {
                List<Activity> activities = Serialization.GetClone(gameActivities.FilterItems);
                DateTime dtLastActivity = activities
                    .Select(x => (DateTime)x.DateSession?.ToLocalTime())
                    .Max()
                    .AddDays(7 * variateurTime);

                List<string> labels = new List<string>();
                ChartValues<CustomerForTime> seriesData = new ChartValues<CustomerForTime>();
                List<WeekStartEnd> datesPeriodes = new List<WeekStartEnd>();

                for (int i = limit; i >= 0; i--)
                {
                    DateTime dt = dtLastActivity.AddDays(-7 * i).ToLocalTime();
                    int weekNumber = UtilityTools.WeekOfYearISO8601(dt);
                    DateTime first = dt.StartOfWeek(DayOfWeek.Monday);
                    string dataTitleInfo = $"{ResourceProvider.GetString("LOCGameActivityWeekLabel")} {weekNumber}";

                    labels.Add(dataTitleInfo);
                    datesPeriodes.Add(new WeekStartEnd
                    {
                        Monday = first,
                        Sunday = first.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59)
                    });
                    seriesData.Add(new CustomerForTime { Name = dataTitleInfo, Values = 0, HideIsZero = true });
                }

                activities.ForEach(x =>
                {
                    DateTime sessionTime = ((DateTime)x.DateSession).ToLocalTime();
                    int idx = datesPeriodes.FindIndex(y => y.Monday <= sessionTime && y.Sunday >= sessionTime);
                    if (idx > -1)
                    {
                        seriesData[idx].Values += (long)x.ElapsedSeconds;
                    }
                });

                SeriesCollection activityForGameSeries = new SeriesCollection();
                activityForGameSeries.Add(new ColumnSeries { Title = "1", Values = seriesData });

                CartesianMapper<CustomerForTime> customerVmMapper = Mappers.Xy<CustomerForTime>()
                    .X((value, index) => index)
                    .Y(value => value.Values);
                Charting.For<CustomerForTime>(customerVmMapper);

                PlayTimeToStringConverterWithZero converter = new PlayTimeToStringConverterWithZero();
                PART_ChartTimeActivityLabelsY.LabelFormatter = value => (string)converter.Convert((ulong)value, null, null, CultureInfo.CurrentCulture);

                PART_ChartTimeActivity.DataTooltip = new CustomerToolTipForTime
                {
                    ShowIcon = PluginDatabase.PluginSettings.Settings.ShowLauncherIcons,
                    Mode = PluginDatabase.PluginSettings.Settings.ModeStoreIcon == 1
                        ? TextBlockWithIconMode.IconTextFirstWithText
                        : TextBlockWithIconMode.IconFirstWithText,
                    DatesPeriodes = datesPeriodes,
                    ShowWeekPeriode = true,
                    ShowTitle = true
                };

                PART_ChartTimeActivityLabelsY.MinValue = 0;
                PART_ChartTimeActivity.Series = activityForGameSeries;
                PART_ChartTimeActivityLabelsX.Labels = labels;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }


        #region Events
        private void PART_ChartTimeActivity_DataClick(object sender, ChartPoint chartPoint)
        {
            GameSeriesDataClick?.Invoke(this, chartPoint);
        }
        #endregion
    }


    public class PluginChartTimeDataContext : ObservableObject, IDataContext
    {
        private bool _isActivated;
        public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

        private double _chartTimeHeight;
        public double ChartTimeHeight { get => _chartTimeHeight; set => SetValue(ref _chartTimeHeight, value); }

        private bool _chartTimeAxis;
        public bool ChartTimeAxis { get => _chartTimeAxis; set => SetValue(ref _chartTimeAxis, value); }

        private bool _chartTimeOrdinates;
        public bool ChartTimeOrdinates { get => _chartTimeOrdinates; set => SetValue(ref _chartTimeOrdinates, value); }

        private bool _chartTimeVisibleEmpty;
        public bool ChartTimeVisibleEmpty { get => _chartTimeVisibleEmpty; set => SetValue(ref _chartTimeVisibleEmpty, value); }

        private bool _disableAnimations = true;
        public bool DisableAnimations { get => _disableAnimations; set => SetValue(ref _disableAnimations, value); }

        private double _labelsRotationValue;
        public double LabelsRotationValue { get => _labelsRotationValue; set => SetValue(ref _labelsRotationValue, value); }
    }
}