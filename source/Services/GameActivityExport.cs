using System;
using System.Collections.Generic;
using CommonPluginsShared.Plugins;
using GameActivity.Models;
using Playnite.SDK;

namespace GameActivity.Services
{
	public class GameActivityExport : PluginExportCsv<GameActivities>
	{
		protected override Dictionary<string, string> GetHeader()
		{
			return new Dictionary<string, string>
			{
				{ "GameName", ResourceProvider.GetString("LOCGameNameTitle") },
				{ "Source", ResourceProvider.GetString("LOCSourceLabel") },
				{ "DateSession", ResourceProvider.GetString("LOCGameActivityDateSession") },
				{ "DateLog", ResourceProvider.GetString("LOCGameActivityDateLog") },
				{ "PlaytimeSeconds", ResourceProvider.GetString("LOCTimePlayed") },
				{ "PlaytimeFormatted", ResourceProvider.GetString("LOCTimePlayed") },
				{ "PCName", ResourceProvider.GetString("LOCGameActivityPCName") },
				{ "CpuUsage", ResourceProvider.GetString("LOCGameActivityCpuUsage") },
				{ "RamUsage", ResourceProvider.GetString("LOCGameActivityRamUsage") },
				{ "GpuUsage", ResourceProvider.GetString("LOCGameActivityGpuUsage") },
				{ "Fps", ResourceProvider.GetString("LOCGameActivityFps") },
				{ "CpuTemp", ResourceProvider.GetString("LOCGameActivityCpuTemp") },
				{ "GpuTemp", ResourceProvider.GetString("LOCGameActivityGpuTemp") },
				{ "CpuPower", ResourceProvider.GetString("LOCGameActivityCpuPower") },
				{ "GpuPower", ResourceProvider.GetString("LOCGameActivityGpuPower") }
			};
		}

		protected override IEnumerable<Dictionary<string, string>> GetRows(GameActivities item)
		{
			var rows = new List<Dictionary<string, string>>();

			foreach (var session in item.Items)
			{
				// Retrieve performance details for this specific session
				var sessionDetails = item.GetSessionActivityDetails(session.DateSession);

				// If session has detailed logs, we create one row per log entry
				if (sessionDetails != null && sessionDetails.Count > 0)
				{
					foreach (var log in sessionDetails)
					{
						rows.Add(CreateRow(item, session, log));
					}
				}
				else
				{
					// If no logs, we create a single row for the session summary
					rows.Add(CreateRow(item, session, null));
				}
			}

			return rows;
		}

		private Dictionary<string, string> CreateRow(GameActivities game, Activity session, ActivityDetailsData log)
		{
			return new Dictionary<string, string>
			{
				{ "GameName", game.Name },
				{ "Source", game.Game?.Source?.Name ?? "Playnite" },
				{ "DateSession", session.DateSession.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") },
				{ "DateLog", log?.Datelog?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty },
				{ "PlaytimeSeconds", session.ElapsedSeconds.ToString() },
				{ "PlaytimeFormatted", FormatTimeSpan(TimeSpan.FromSeconds(session.ElapsedSeconds)) },
				{ "PCName", session.Configuration?.Name ?? string.Empty },
				{ "CpuUsage", log?.CPU.ToString() ?? string.Empty },
				{ "RamUsage", log?.RAM.ToString() ?? string.Empty },
				{ "GpuUsage", log?.GPU.ToString() ?? string.Empty },
				{ "Fps", log?.FPS.ToString() ?? string.Empty },
				{ "CpuTemp", log?.CPUT.ToString() ?? string.Empty },
				{ "GpuTemp", log?.GPUT.ToString() ?? string.Empty },
				{ "CpuPower", log?.CPUP.ToString() ?? string.Empty },
				{ "GpuPower", log?.GPUP.ToString() ?? string.Empty }
			};
		}

		private string FormatTimeSpan(TimeSpan ts)
		{
			return string.Format("{0:00}:{1:00}:{2:00}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
		}
	}
}