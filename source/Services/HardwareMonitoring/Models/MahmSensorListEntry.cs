using System.Text;

namespace GameActivity.Services.HardwareMonitoring.Models
{
	/// <summary>
	/// One MAHM monitoring row for display in settings (source name + optional hardware / units).
	/// <see cref="SourceName"/> is the string persisted in plugin settings and matched against MAHM at runtime.
	/// </summary>
	public sealed class MahmSensorListEntry
	{
		public MahmSensorListEntry(string sourceName, string hardwareContext, string units)
		{
			SourceName = sourceName ?? string.Empty;
			HardwareContext = hardwareContext ?? string.Empty;
			Units = units ?? string.Empty;
			DisplayLine = FormatDisplayLine(SourceName, HardwareContext, Units);
		}

		public string SourceName { get; }
		public string HardwareContext { get; }
		public string Units { get; }
		public string DisplayLine { get; }

		private static string FormatDisplayLine(string sourceName, string hardwareContext, string units)
		{
			var sb = new StringBuilder();
			sb.Append(sourceName);
			if (!string.IsNullOrWhiteSpace(hardwareContext))
			{
				sb.Append(" — ");
				sb.Append(hardwareContext.Trim());
			}
			if (!string.IsNullOrWhiteSpace(units))
			{
				sb.Append(" (");
				sb.Append(units.Trim());
				sb.Append(")");
			}
			return sb.ToString();
		}
	}
}
