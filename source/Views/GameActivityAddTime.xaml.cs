using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
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


        public GameActivityAddTime(Game game)
        {
            this.game = game;

            InitializeComponent();
        }


        private void PART_Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime DateStart = DateTime.Parse(((DateTime)PART_DateStart.SelectedDate).ToString("yyyy-MM-dd") + " " + PART_TimeStart.GetValueAsString() + ":00");
                DateTime DateEnd = DateTime.Parse(((DateTime)PART_DateEnd.SelectedDate).ToString("yyyy-MM-dd") + " " + PART_TimeEnd.GetValueAsString() + ":00");

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
            PART_Add.IsEnabled = !PART_DateStart.Text.IsNullOrEmpty() && !PART_DateEnd.Text.IsNullOrEmpty();
        }
    }
}
