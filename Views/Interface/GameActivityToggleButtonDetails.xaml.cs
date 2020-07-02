using System;
using System.Windows.Controls.Primitives;


namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GameActivityToggleButtonDetails.xaml
    /// </summary>
    public partial class GameActivityToggleButtonDetails : ToggleButton
    {
        public GameActivityToggleButtonDetails(long Playtime)
        {
            InitializeComponent();

            ga_labelButtonData.Content = (int)TimeSpan.FromSeconds(Playtime).TotalHours + "h " + TimeSpan.FromSeconds(Playtime).ToString(@"mm") + "min";
        }
    }
}
