using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace GameActivity.Views
{
	/// <summary>Session logging settings section.</summary>
	public partial class GaMonitoringLoggingSettingsSection : UserControl
	{
		/// <summary>Initializes a new instance of the <see cref="GaMonitoringLoggingSettingsSection"/> class.</summary>
		public GaMonitoringLoggingSettingsSection()
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
