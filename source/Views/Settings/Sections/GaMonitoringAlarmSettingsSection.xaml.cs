using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace GameActivity.Views
{
	/// <summary>In-game alarm channel settings section.</summary>
	public partial class GaMonitoringAlarmSettingsSection : UserControl
	{
		/// <summary>Initializes a new instance of the <see cref="GaMonitoringAlarmSettingsSection"/> class.</summary>
		public GaMonitoringAlarmSettingsSection()
		{
			InitializeComponent();
		}

		private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}
	}
}
