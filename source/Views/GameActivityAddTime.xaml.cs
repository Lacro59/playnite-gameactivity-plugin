using CommonPlayniteShared.Converters;
using GameActivity.Models;
using GameActivity.Services;
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
        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        public Activity activity;
        private Game game;

        private PlayTimeToStringConverter playTimeToStringConverter = new PlayTimeToStringConverter();


        public GameActivityAddTime(Game game, Activity activity)
        {
            this.game = game;

            InitializeComponent();

            PART_ElapseTime.Content = "--";
            if (activity != null)
            {
                var DateSessionStart = ((DateTime)activity.DateSession).ToLocalTime();
                PART_DateStart.SelectedDate = activity.DateSession;
                PART_TimeStart.SetValueAsString(DateSessionStart.ToString("HH"), DateSessionStart.ToString("mm"), DateSessionStart.ToString("ss"));

                var DateSessionEnd = DateSessionStart.AddSeconds(activity.ElapsedSeconds);
                PART_DateEnd.SelectedDate = DateSessionEnd;
                PART_TimeEnd.SetValueAsString(DateSessionEnd.ToString("HH"), DateSessionEnd.ToString("mm"), DateSessionEnd.ToString("ss"));

                PART_DateStart.IsEnabled = false;
                PART_TimeStart.IsEnabled = false;

                SetElapsedTime();
            }
        }


        private void PART_Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime DateStart = DateTime.Parse(((DateTime)PART_DateStart.SelectedDate).ToString("yyyy-MM-dd") + " " + PART_TimeStart.GetValueAsString());
                DateTime DateEnd = DateTime.Parse(((DateTime)PART_DateEnd.SelectedDate).ToString("yyyy-MM-dd") + " " + PART_TimeEnd.GetValueAsString());

                activity = new Activity
                {
                    DateSession = DateStart.ToUniversalTime(),
                    ElapsedSeconds = (ulong)(DateEnd - DateStart).TotalSeconds,
                    IdConfiguration = PluginDatabase.LocalSystem.GetIdConfiguration(),
                    PlatformIDs = game.PlatformIds,
                    SourceID = game.SourceId
                };
            }
            catch { }

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
