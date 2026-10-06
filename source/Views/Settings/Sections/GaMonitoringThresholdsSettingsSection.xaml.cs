using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace GameActivity.Views
{
	/// <summary>Shared threshold / indicator settings section.</summary>
	public partial class GaMonitoringThresholdsSettingsSection : UserControl
	{
		/// <summary>Initializes a new instance of the <see cref="GaMonitoringThresholdsSettingsSection"/> class.</summary>
		public GaMonitoringThresholdsSettingsSection()
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
