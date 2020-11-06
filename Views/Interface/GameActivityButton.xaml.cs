using System.Windows;
using System.Windows.Controls;

namespace GameActivity.Views.Interface
{
    /// <summary>
    /// Logique d'interaction pour GameActivityButton.xaml
    /// </summary>
    public partial class GameActivityButton : Button
    {
        public GameActivityButton(bool EnableIntegrationInDescriptionOnlyIcon)
        {
            InitializeComponent();

            if (EnableIntegrationInDescriptionOnlyIcon)
            {
                PART_ButtonIcon.Visibility = Visibility.Visible;
                PART_ButtonText.Visibility = Visibility.Collapsed;
            }
            else
            {
                PART_ButtonIcon.Visibility = Visibility.Collapsed;
                PART_ButtonText.Visibility = Visibility.Visible;
            }
        }
    }
}
