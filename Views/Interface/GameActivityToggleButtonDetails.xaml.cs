using System;
using System.Windows.Controls.Primitives;


namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GameActivityToggleButtonDetails.xaml
    /// </summary>
    public partial class GameActivityToggleButtonDetails : ToggleButton
    {
        public long PlaytimeCurrent { get; set; }

        public GameActivityToggleButtonDetails(long Playtime)
        {
            InitializeComponent();

            PlaytimeCurrent = Playtime;

            DataContext = this;
        }
    }
}
