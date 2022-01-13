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

namespace GameActivity.Views
{
    /// <summary>
    /// Logique d'interaction pour GameActivityAddTime.xaml
    /// </summary>
    public partial class GameActivityAddTime : UserControl
    {
        internal static IResourceProvider resources = new ResourceProvider();

        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        public Activity activity;
        public Activity activityEdit = new Activity();
        private Game game;

        private PlayTimeToStringConverter playTimeToStringConverter = new PlayTimeToStringConverter();


        public GameActivityAddTime(Game game, Activity activityEdit)
        {
            this.game = game;

            InitializeComponent();

            PART_ElapseTime.Content = "--";
            PART_CbPlayAction.ItemsSource = game.GameActions?.Select(x => x.Name.IsNullOrEmpty() ? resources.GetString("LOCGameActivityDefaultAction") : x.Name)?.ToList() ?? new List<string> { ResourceProvider.GetString("LOCGameActivityDefaultAction") };

            if (activityEdit != null)
            {
                var DateSessionStart = ((DateTime)activityEdit.DateSession).ToLocalTime();
                PART_DateStart.SelectedDate = DateSessionStart;
                PART_TimeStart.SetValueAsString(DateSessionStart.ToString("HH"), DateSessionStart.ToString("mm"), DateSessionStart.ToString("ss"));

                var DateSessionEnd = DateSessionStart.AddSeconds(activityEdit.ElapsedSeconds);
                PART_DateEnd.SelectedDate = DateSessionEnd;
                PART_TimeEnd.SetValueAsString(DateSessionEnd.ToString("HH"), DateSessionEnd.ToString("mm"), DateSessionEnd.ToString("ss"));

                PART_DateStart.IsEnabled = false;
                PART_TimeStart.IsEnabled = false;

                PART_CbPlayAction.Text = activityEdit.GameActionName;

                var playAction = ((List<string>)PART_CbPlayAction.ItemsSource)?.Find(x => x.IsEqual(activityEdit.GameActionName)) ?? null;
                if (playAction != null)
                {
                    PART_CbPlayAction.SelectedItem = playAction;
                }
                else
                {
                    ((List<string>)PART_CbPlayAction.ItemsSource).Add(activityEdit.GameActionName);
                    playAction = ((List<string>)PART_CbPlayAction.ItemsSource)?.Find(x => x.IsEqual(activityEdit.GameActionName)) ?? null;
                    PART_CbPlayAction.SelectedItem = playAction;
                }

                SetElapsedTime();

                PART_Add.Content = resources.GetString("LOCSaveLabel");
            }

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
                activity.GameActionName = PART_CbPlayAction.Text;
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
    }
}
