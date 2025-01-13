using CommonPlayniteShared.Converters;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GameActivity.Views
{
    /// <summary>
    /// Logique d'interaction pour GameActivityAddTime.xaml
    /// </summary>
    public partial class GameActivityAddTime : UserControl
    {
        private static ActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

        public GameActivity Plugin { get; set; }
        public Activity Activity { get; set; }
        public Activity ActivityEdit { get; set; } = new Activity();
        private Game GameContext { get; set; }

        private List<CbListHeader> CbListHeaders { get; set; }

        private PlayTimeToStringConverter PlayTimeToStringConverter => new PlayTimeToStringConverter();


        public GameActivityAddTime(GameActivity plugin, Game game, Activity activityEdit)
        {
            Plugin = plugin;
            GameContext = game;

            InitializeComponent();

            PART_ElapseTime.Content = "--";
            DateTime dt = DateTime.Now;
            PART_DateStart.SelectedDate = dt;
            PART_TimeStart.SetValueAsString(dt.ToString("HH"), dt.ToString("mm"), dt.ToString("ss"));
            PART_DateEnd.SelectedDate = dt;
            PART_TimeEnd.SetValueAsString(dt.ToString("HH"), dt.ToString("mm"), dt.ToString("ss"));

            List<string> listCb = game.GameActions?.Select(x => x.Name.IsNullOrEmpty() ? ResourceProvider.GetString("LOCGameActivityDefaultAction") : x.Name)?.ToList() ?? new List<string> { ResourceProvider.GetString("LOCGameActivityDefaultAction") };
            _ = PluginDatabase.PluginSettings.Settings.CustomGameActions.TryGetValue(game.Id, out List<string> listCbCustom);

            CbListHeaders = listCb.Select(x => new CbListHeader { Name = x, Category = ResourceProvider.GetString("LOCGaGameActions") }).ToList();
            if (listCbCustom != null)
            {
                List<CbListHeader> tmp = listCbCustom.Select(x => new CbListHeader { Name = x, Category = ResourceProvider.GetString("LOCGaCustomGameActions"), IsCustom = true }).ToList();
                CbListHeaders = CbListHeaders.Concat(tmp).ToList();
            }


            CbListHeader playAction = null;
            if (activityEdit != null)
            {
                DateTime DateSessionStart = ((DateTime)activityEdit.DateSession).ToLocalTime();
                PART_DateStart.SelectedDate = DateSessionStart;
                PART_TimeStart.SetValueAsString(DateSessionStart.ToString("HH"), DateSessionStart.ToString("mm"), DateSessionStart.ToString("ss"));

                DateTime DateSessionEnd = DateSessionStart.AddSeconds(activityEdit.ElapsedSeconds);
                PART_DateEnd.SelectedDate = DateSessionEnd;
                PART_TimeEnd.SetValueAsString(DateSessionEnd.ToString("HH"), DateSessionEnd.ToString("mm"), DateSessionEnd.ToString("ss"));

                PART_DateStart.IsEnabled = false;
                PART_TimeStart.IsEnabled = false;

                PART_CbPlayAction.Text = activityEdit.GameActionName;

                playAction = CbListHeaders?.Find(x => x.Name.IsEqual(activityEdit.GameActionName)) ?? null;
                if (playAction == null)
                {
                    CbListHeaders.Add(new CbListHeader { Name = activityEdit.GameActionName, Category = ResourceProvider.GetString("LOCOther") });
                    playAction = CbListHeaders?.Find(x => x.Name.IsEqual(activityEdit.GameActionName)) ?? null;
                }

                SetElapsedTime();

                PART_Add.Content = ResourceProvider.GetString("LOCSaveLabel");
            }

            ListCollectionView lcv = new ListCollectionView(CbListHeaders);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

            PART_CbPlayAction.ItemsSource = lcv;
            PART_CbPlayAction.SelectedItem = playAction;

            ActivityEdit = activityEdit;
        }


        private void PART_Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime DateStart = DateTime.Parse(((DateTime)PART_DateStart.SelectedDate).ToString("yyyy-MM-dd") + " " + PART_TimeStart.GetValueAsString());
                DateTime DateEnd = DateTime.Parse(((DateTime)PART_DateEnd.SelectedDate).ToString("yyyy-MM-dd") + " " + PART_TimeEnd.GetValueAsString());

                Activity = PART_DateStart.IsEnabled ? new Activity { DateSession = DateStart.ToUniversalTime() } : ActivityEdit;
                Activity.GameActionName = ((CbListHeader)PART_CbPlayAction.SelectedItem)?.Name ?? ResourceProvider.GetString("LOCGameActivityDefaultAction");
                Activity.ElapsedSeconds = (ulong)(DateEnd - DateStart).TotalSeconds;
                Activity.IdConfiguration = PluginDatabase.LocalSystem.GetIdConfiguration();
                Activity.PlatformIDs = GameContext.PlatformIds;
                Activity.SourceID = GameContext.SourceId;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            ((Window)Parent).Close();
        }

        private void PART_Cancel_Click(object sender, RoutedEventArgs e)
        {
            ((Window)Parent).Close();
        }


        private void PART_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            SetElapsedTime();
        }

        private void PART_TimeChanged(object sender, RoutedEventArgs e)
        {
            SetElapsedTime();
        }


        private void SetElapsedTime()
        {
            try
            {
                DateTime DateStart = DateTime.Parse(((DateTime)PART_DateStart.SelectedDate).ToString("yyyy-MM-dd") + " " + PART_TimeStart.GetValueAsString());
                DateTime DateEnd = DateTime.Parse(((DateTime)PART_DateEnd.SelectedDate).ToString("yyyy-MM-dd") + " " + PART_TimeEnd.GetValueAsString());

                ulong TotalSeconds = DateEnd <= DateStart ? 0 : (ulong)(DateEnd - DateStart).TotalSeconds;
                PART_ElapseTime.Content = (string)PlayTimeToStringConverter.Convert(TotalSeconds, null, null, CultureInfo.CurrentCulture);
                PART_Add.IsEnabled = TotalSeconds > 0;
            }
            catch
            {
                PART_ElapseTime.Content = "--";
                PART_Add.IsEnabled = false;
            }
        }


        #region PlayAction custom
        private void ButtonAddPlayAction_Click_1(object sender, RoutedEventArgs e)
        {
            PART_ContextMenuPlayAction.Visibility = Visibility.Visible;
        }

        private void ButtonAddPlayAction_Click(object sender, RoutedEventArgs e)
        {
            if (!PART_PlayActionLabel.Text.IsNullOrEmpty())
            {
                CbListHeaders.Add(new CbListHeader { Name = PART_PlayActionLabel.Text, Category = ResourceProvider.GetString("LOCGaCustomGameActions"), IsCustom = true });

                ListCollectionView lcv = new ListCollectionView(CbListHeaders);
                lcv.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

                PART_CbPlayAction.ItemsSource = null;
                PART_CbPlayAction.ItemsSource = lcv;

                // Save in settings
                _ = PluginDatabase.PluginSettings.Settings.CustomGameActions.TryGetValue(GameContext.Id, out List<string> listCbCustom);
                if (listCbCustom == null)
                {
                    listCbCustom = new List<string>();
                }
                listCbCustom.Add(PART_PlayActionLabel.Text);
                PluginDatabase.PluginSettings.Settings.CustomGameActions[GameContext.Id] = listCbCustom;
                Plugin.SavePluginSettings(PluginDatabase.PluginSettings.Settings);

                PART_PlayActionLabel.Text = string.Empty;
            }

            PART_ContextMenuPlayAction.Visibility = Visibility.Collapsed;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button bt = sender as Button;
            CbListHeader finded = CbListHeaders.Find(x => x.Name.IsEqual(bt.Tag.ToString()));
            if (finded != null)
            {
                _ = CbListHeaders.Remove(finded);

                ListCollectionView lcv = new ListCollectionView(CbListHeaders);
                lcv.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

                PART_CbPlayAction.ItemsSource = null;
                PART_CbPlayAction.ItemsSource = lcv;

                // Save in settings
                _ = PluginDatabase.PluginSettings.Settings.CustomGameActions.TryGetValue(GameContext.Id, out List<string> listCbCustom);
                if (listCbCustom == null)
                {
                    listCbCustom = new List<string>();
                }
                _ = listCbCustom.Remove(finded.Name);
                PluginDatabase.PluginSettings.Settings.CustomGameActions[GameContext.Id] = listCbCustom;
                Plugin.SavePluginSettings(PluginDatabase.PluginSettings.Settings);
            }
        }

        private void StackPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ContextMenu el = UI.FindParent<ContextMenu>((FrameworkElement)sender);
            foreach (var ui in UI.FindVisualChildren<Border>(el))
            {
                if (((FrameworkElement)ui).Name == "HoverBorder")
                {
                    ((Border)ui).Background = (System.Windows.Media.Brush)ResourceProvider.GetResource("NormalBrush");
                    break;
                }
            }
        }
        private void PART_CbPlayAction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Part_PlayActionDelete.IsEnabled = false;
            if (PART_CbPlayAction.SelectedItem != null && ((CbListHeader)PART_CbPlayAction.SelectedItem).IsCustom)
            {
                Part_PlayActionDelete.IsEnabled = true;
                Part_PlayActionDelete.Tag = ((CbListHeader)PART_CbPlayAction.SelectedItem).Name;
            }
        }
        #endregion
    }

    public class CbListHeader
    {
        public string Category { get; set; }
        public string Name { get; set; }
        public bool IsCustom { get; set; }
    }
}
