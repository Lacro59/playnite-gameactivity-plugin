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
        internal static IResourceProvider resources = new ResourceProvider();

        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        public GameActivity plugin { get; set; }
        public Activity activity { get; set; }
        public Activity activityEdit { get; set; } = new Activity();
        private Game game { get; set; }

        private List<CbListHeader> cbListHeaders { get; set; }

        private PlayTimeToStringConverter playTimeToStringConverter = new PlayTimeToStringConverter();


        public GameActivityAddTime(GameActivity plugin, Game game, Activity activityEdit)
        {
            this.plugin = plugin;
            this.game = game;

            InitializeComponent();

            PART_ElapseTime.Content = "--";

            List<string> listCb = game.GameActions?.Select(x => x.Name.IsNullOrEmpty() ? resources.GetString("LOCGameActivityDefaultAction") : x.Name)?.ToList() ?? new List<string> { ResourceProvider.GetString("LOCGameActivityDefaultAction") };
            PluginDatabase.PluginSettings.Settings.CustomGameActions.TryGetValue(game.Id, out List<string> listCbCustom);

            cbListHeaders = listCb.Select(x => new CbListHeader { Name = x, Category = resources.GetString("LOCGaGameActions") }).ToList();
            if (listCbCustom != null) 
            {
                List<CbListHeader> tmp = listCbCustom.Select(x => new CbListHeader { Name = x, Category = resources.GetString("LOCGaCustomGameActions"), IsCustom = true }).ToList();
                cbListHeaders = cbListHeaders.Concat(tmp).ToList();
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

                playAction = cbListHeaders?.Find(x => x.Name.IsEqual(activityEdit.GameActionName)) ?? null;
                if (playAction == null)
                {
                    cbListHeaders.Add(new CbListHeader { Name = activityEdit.GameActionName, Category = resources.GetString("LOCOther") });
                    playAction = cbListHeaders?.Find(x => x.Name.IsEqual(activityEdit.GameActionName)) ?? null;
                }

                SetElapsedTime();

                PART_Add.Content = resources.GetString("LOCSaveLabel");
            }

            ListCollectionView lcv = new ListCollectionView(cbListHeaders);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

            PART_CbPlayAction.ItemsSource = lcv;
            PART_CbPlayAction.SelectedItem = playAction;

            this.activityEdit = activityEdit;
        }


        private void PART_Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime DateStart = DateTime.Parse(((DateTime)PART_DateStart.SelectedDate).ToString("yyyy-MM-dd") + " " + PART_TimeStart.GetValueAsString());
                DateTime DateEnd = DateTime.Parse(((DateTime)PART_DateEnd.SelectedDate).ToString("yyyy-MM-dd") + " " + PART_TimeEnd.GetValueAsString());

                if (PART_DateStart.IsEnabled)
                {
                    activity = new Activity();
                    activity.DateSession = DateStart.ToUniversalTime();
                }
                else
                {
                    activity = activityEdit;
                }
                activity.GameActionName = ((CbListHeader)PART_CbPlayAction.SelectedItem).Name;
                activity.ElapsedSeconds = (ulong)(DateEnd - DateStart).TotalSeconds;
                activity.IdConfiguration = PluginDatabase.LocalSystem.GetIdConfiguration();
                activity.PlatformIDs = game.PlatformIds;
                activity.SourceID = game.SourceId;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            ((Window)this.Parent).Close();
        }

        private void PART_Cancel_Click(object sender, RoutedEventArgs e)
        {
            ((Window)this.Parent).Close();
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
                PART_ElapseTime.Content = (string)playTimeToStringConverter.Convert((ulong)(DateEnd - DateStart).TotalSeconds, null, null, CultureInfo.CurrentCulture);

                PART_Add.IsEnabled = true;
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
                cbListHeaders.Add(new CbListHeader { Name = PART_PlayActionLabel.Text, Category = resources.GetString("LOCGaCustomGameActions"), IsCustom = true });

                ListCollectionView lcv = new ListCollectionView(cbListHeaders);
                lcv.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

                PART_CbPlayAction.ItemsSource = null;
                PART_CbPlayAction.ItemsSource = lcv;

                // Save in settings
                PluginDatabase.PluginSettings.Settings.CustomGameActions.TryGetValue(game.Id, out List<string> listCbCustom);
                if (listCbCustom == null)
                {
                    listCbCustom = new List<string>();
                }
                listCbCustom.Add(PART_PlayActionLabel.Text);
                PluginDatabase.PluginSettings.Settings.CustomGameActions[game.Id] = listCbCustom;
                plugin.SavePluginSettings(PluginDatabase.PluginSettings.Settings);

                PART_PlayActionLabel.Text = string.Empty;
            }

            PART_ContextMenuPlayAction.Visibility = Visibility.Collapsed;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button bt = sender as Button;
            CbListHeader finded = cbListHeaders.Find(x => x.Name.IsEqual(bt.Tag.ToString()));
            if (finded != null)
            {
                cbListHeaders.Remove(finded);

                ListCollectionView lcv = new ListCollectionView(cbListHeaders);
                lcv.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

                PART_CbPlayAction.ItemsSource = null;
                PART_CbPlayAction.ItemsSource = lcv;

                // Save in settings
                PluginDatabase.PluginSettings.Settings.CustomGameActions.TryGetValue(game.Id, out List<string> listCbCustom);
                if (listCbCustom == null)
                {
                    listCbCustom = new List<string>();
                }
                listCbCustom.Remove(finded.Name);
                PluginDatabase.PluginSettings.Settings.CustomGameActions[game.Id] = listCbCustom;
                plugin.SavePluginSettings(PluginDatabase.PluginSettings.Settings);
            }
        }

        private void StackPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ContextMenu el = UI.FindParent<ContextMenu>((FrameworkElement)sender);
            foreach (var ui in UI.FindVisualChildren<Border>(el))
            {
                if (((FrameworkElement)ui).Name == "HoverBorder")
                {
                    ((Border)ui).Background = (System.Windows.Media.Brush)resources.GetResource("NormalBrush");
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
