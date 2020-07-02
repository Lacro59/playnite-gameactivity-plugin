using System;
using System.Windows.Controls;


namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GameActivityButtonDetails.xaml
    /// </summary>
    public partial class GameActivityButtonDetails : Button
    {
        public GameActivityButtonDetails(long Playtime)
        {
            InitializeComponent();

            ga_labelButtonData.Content = (int)TimeSpan.FromSeconds(Playtime).TotalHours + "h " + TimeSpan.FromSeconds(Playtime).ToString(@"mm") + "min";
        }
    }
}
