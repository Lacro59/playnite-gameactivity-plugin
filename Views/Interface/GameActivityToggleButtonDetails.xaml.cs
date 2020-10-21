using PluginCommon.PlayniteResources.Converters;
using System;
using System.Globalization;
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

        public void SetGaData(long Playtime)
        {
            LongToTimePlayedConverter converter = new LongToTimePlayedConverter();
            string PlaytimeString = (string)converter.Convert(Playtime, null, null, CultureInfo.CurrentCulture);
            PART_GaButtonPlaytime.Content = PlaytimeString;
        }
    }
}
