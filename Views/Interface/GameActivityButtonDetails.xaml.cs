using PluginCommon.PlayniteResources.Converters;
using System;
using System.Globalization;
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

        public void SetGaData(long Playtime)
        {
            LongToTimePlayedConverter converter = new LongToTimePlayedConverter();
            string PlaytimeString = (string)converter.Convert(Playtime, null, null, CultureInfo.CurrentCulture);
            PART_GaButtonPlaytime.Content = PlaytimeString;
        }
    }
}
