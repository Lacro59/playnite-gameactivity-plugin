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
			if (item?.Items == null)
			{
				return rows;
			}

			foreach (Activity session in item.Items)
			{
				rows.AddRange(GetRowsForSingleSession(item, session));
			}

			return rows;
		}

		/// <summary>
		/// Builds CSV rows for one session (one row per detail log, or a single summary row when there are no logs).
		/// </summary>
		protected List<Dictionary<string, string>> GetRowsForSingleSession(GameActivities item, Activity session)
		{
			var rows = new List<Dictionary<string, string>>();
			if (item == null || session == null)
			{
				return rows;
			}

			List<ActivityDetailsData> sessionDetails = item.GetSessionActivityDetails(session.DateSession);
			if (sessionDetails != null && sessionDetails.Count > 0)
			{
				foreach (ActivityDetailsData log in sessionDetails)
				{
					rows.Add(BuildActivityCsvRow(item, session, log));
				}
			}
			else
			{
				rows.Add(BuildActivityCsvRow(item, session, null));
			}

			return rows;
		}

		/// <summary>
		/// Builds one CSV row for a session (and optional per-log sample).
		/// </summary>
		protected Dictionary<string, string> BuildActivityCsvRow(GameActivities game, Activity session, ActivityDetailsData log)
		{
			return new Dictionary<string, string>
			{
				{ "GameName", game.Name },
				{ "Source", game.Game?.Source?.Name ?? "Playnite" },
				{ "DateSession", FormatCsvUtcDateTime(session.DateSession) },
				{ "DateLog", FormatCsvUtcDateTime(log?.Datelog) },
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

		protected string FormatTimeSpan(TimeSpan ts)
		{
			return string.Format("{0:00}:{1:00}:{2:00}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
		}
	}
}