using CommonPluginsControls.Controls;
using GameActivity.Controls;
using GameActivity.Models;
using GameActivity.Services;
using GameActivity.ViewModels;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace GameActivity.Views
{
    /// <summary>
    /// Code-behind for GameActivityViewSingle.
    /// Responsibility is limited to:
    ///   1. Constructing and assigning the ViewModel.
    ///   2. Wiring chart controls that cannot be bound via XAML (PluginChartTime / PluginChartLog).
    ///   3. Configuring ListView column visibility from plugin settings.
    ///   4. Relaying UI-only events (navigation buttons, selection) to the chart controls.
    /// All data logic and commands live in GameActivityViewSingleViewModel.
    /// </summary>
    public partial class GameActivityViewSingle : UserControl
    {
        private static ILogger Logger => LogManager.GetLogger();
        private GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

        // Chart control references resolved after InitializeComponent.
        private PluginChartTime _chartTime;
        private PluginChartLog _chartLog;

        private GameActivityViewSingleViewModel ViewModel => DataContext as GameActivityViewSingleViewModel;

        // ─── Constructor ──────────────────────────────────────────────────────────────

        public GameActivityViewSingle(GameActivity plugin, Game game)
        {
            InitializeComponent();

            // Assign ViewModel — all bindings resolve from this point.
            DataContext = new GameActivityViewSingleViewModel(plugin, game);

            // Resolve chart controls injected via ContentControl children in XAML.
            _chartTime = (PluginChartTime)PART_ChartTimeContainer.Children[0];
            _chartTime.GameContext = game;
            _chartTime.Truncate = PluginDatabase.PluginSettings.ChartTimeTruncate;

            _chartLog = (PluginChartLog)PART_LogSection.Child;
            _chartLog.GameContext = game;

            // Configure ListView column visibility.
            ConfigureListViewColumns();

            // ListView persistence.
            lvSessions.EnableColumnPersistence = PluginDatabase.PluginSettings.SaveColumnOrder;
            lvSessions.ColumnConfigurationFilePath = System.IO.Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "ListViewColumns.json");
            lvSessions.ColumnConfigurationScope = CommonPluginsShared.Controls.ColumnConfigurationScope.Custom;
            lvSessions.ColumnConfigurationKey = "GameActivityViewSingle.lvSessions";
        }

        // ─── Column Configuration ─────────────────────────────────────────────────────

        /// <summary>
        /// Hides hardware monitoring columns when logging is disabled.
        /// Width=0 + IsHitTestVisible=false is the established pattern in this project.
        /// </summary>
        private void ConfigureListViewColumns()
        {
            if (!PluginDatabase.PluginSettings.EnableLogging)
            {
                // Hide all hardware-monitoring columns when logging is disabled.
                HideColumn(lvAvgGpuP, lvAvgGpuPHeader, true);
                HideColumn(lvAvgCpuP, lvAvgCpuPHeader, true);
                HideColumn(lvAvgGpuT, lvAvgGpuTHeader, true);
                HideColumn(lvAvgCpuT, lvAvgCpuTHeader, true);
                HideColumn(lvSessionFpsStdDev, lvSessionFpsStdDevHeader, true);
                HideColumn(lvSessionFpsMedian, lvSessionFpsMedianHeader, true);
                HideColumn(lvSessionFpsMax, lvSessionFpsMaxHeader, true);
                HideColumn(lvSessionFpsMin, lvSessionFpsMinHeader, true);
                HideColumn(lvAvgFps, lvAvgFpsHeader, true);
                HideColumn(lvAvgRam, lvAvgRamHeader, true);
                HideColumn(lvAvgGpu, lvAvgGpuHeader, true);
                HideColumn(lvAvgCpu, lvAvgCpuHeader, true);

                // Remove log chart area so time chart can use remaining height.
                RowTimeLogSpacer.Height = new GridLength(0);
                RowLogSection.Height = new GridLength(0);
                RowLogExpanderSpacer.Height = new GridLength(0);
                PART_LogSection.Visibility = Visibility.Collapsed;

                // Keep PC config always expanded and non-collapsible without logging.
                PART_PcConfigExpander.IsExpanded = true;
                PART_PcConfigExpander.Collapsed += PcConfigExpander_Collapsed;
            }
        }

        private void PcConfigExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            if (!PluginDatabase.PluginSettings.EnableLogging)
            {
                PART_PcConfigExpander.IsExpanded = true;
            }
        }

        /// <summary>Hides a GridViewColumn by zeroing its width and disabling hit-testing on its header.</summary>
        private static void HideColumn(GridViewColumn column, GridViewColumnHeader header, bool forceHidden = false)
        {
            column.Width = 0;
            header.IsHitTestVisible = false;
            CommonPluginsShared.Controls.ListViewColumnOptions.SetForceHidden(column, forceHidden);
        }

        // ─── ListView Events ──────────────────────────────────────────────────────────

        private void LvSessions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // AddedItems is always reliable — populated before SelectedItem is committed.
            if (e.AddedItems == null || e.AddedItems.Count == 0)
            {
                return;
            }

            ListActivities selected = e.AddedItems[0] as ListActivities;
            if (selected == null)
            {
                return;
            }

            DateTime dateSelected = selected.GameLastActivity;

            _chartLog.DateSelected = dateSelected;
            _chartLog.TitleChart = "1";
            _chartLog.AxisVariator = 0;

            ViewModel?.UpdatePcConfiguration(selected.PCConfigurationId);
        }


        // ─── Chart Time Navigation ────────────────────────────────────────────────────

        private void Bt_PrevTime(object sender, RoutedEventArgs e) => _chartTime.Prev();
        private void Bt_NextTime(object sender, RoutedEventArgs e) => _chartTime.Next();
        private void Bt_PrevTimePlus(object sender, RoutedEventArgs e) => _chartTime.Prev(PluginDatabase.PluginSettings.VariatorTime);
        private void Bt_NextTimePlus(object sender, RoutedEventArgs e) => _chartTime.Next(PluginDatabase.PluginSettings.VariatorTime);

        private void Bt_Truncate(object sender, RoutedEventArgs e)
        {
            _chartTime.Truncate = (bool)((ToggleButton)sender).IsChecked;
            _chartTime.AxisVariator = 0;
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _chartTime.ShowByWeeks = (bool)((ToggleButton)sender).IsChecked;
            _chartTime.AxisVariator = 0;
        }

        // ─── Chart Log Navigation ─────────────────────────────────────────────────────

        private void Bt_PrevLog(object sender, RoutedEventArgs e) => _chartLog.Prev();
        private void Bt_NextLog(object sender, RoutedEventArgs e) => _chartLog.Next();
        private void Bt_PrevLogPlus(object sender, RoutedEventArgs e) => _chartLog.Prev(PluginDatabase.PluginSettings.VariatorLog);
        private void Bt_NextLogPlus(object sender, RoutedEventArgs e) => _chartLog.Next(PluginDatabase.PluginSettings.VariatorLog);
    }
}