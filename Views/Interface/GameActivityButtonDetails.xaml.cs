using System;
using System.Windows.Controls;


namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GameActivityButtonDetails.xaml
    /// </summary>
    public partial class GameActivityButtonDetails : Button
    {
        public long PlaytimeCurrent { get; set; }

        public GameActivityButtonDetails(long Playtime)
        {
            InitializeComponent();

            PlaytimeCurrent = Playtime;

            DataContext = this;
        }
    }
}
