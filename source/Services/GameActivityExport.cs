using System;
using System.Collections.Generic;
using CommonPluginsShared.Plugins;
using GameActivity.Models;
using Playnite.SDK;

namespace GameActivity.Services
{
	public class GameActivityExport : PluginExportCsv<GameActivities>
	{
		/// <summary>
		/// When false, per-session FPS aggregate columns are omitted from the header and from each row
		/// (used by <see cref="GameActivityDetailsExport"/> where each line is already a log sample).
		/// </summary>
		protected virtual bool IncludeSessionFpsAggregateColumns
		{
			get { return true; }
		}

		/// <summary>
		/// When true, the <c>DateLog</c> column is included (per-sample log timestamp on detail export).
		/// </summary>
		protected virtual bool IncludeDateLogColumn
		{
			get { return false; }
		}

		/// <summary>
		/// When true, CSV includes session-level avg/min columns for Framerate 1% / 0.1% Low (from logged samples).
		/// </summary>
		protected virtual bool IncludeFrameratePercentileSummaryColumns
		{
			get { return true; }
		}

		/// <summary>
		/// When true, CSV includes per-sample Framerate 1% / 0.1% Low columns (<see cref="GameActivityDetailsExport"/>).
		/// </summary>
		protected virtual bool IncludeFrameratePercentileRawColumns
		{
			get { return false; }
		}

		/// <summary>
		/// When true, CSV headers use Avg* localization keys (session summary export).
		/// When false, headers use per-sample keys (<see cref="GameActivityDetailsExport"/>).
		/// </summary>
		protected virtual bool UseAverageColumnHeaders
		{
			get { return true; }
		}

		protected override Dictionary<string, string> GetHeader()
		{
			var header = new Dictionary<string, string>
			{
				{ "GameName", ResourceProvider.GetString("LOCGameNameTitle") },
				{ "Source", ResourceProvider.GetString("LOCSourceLabel") },
				{ "DateSession", ResourceProvider.GetString("LOCGameActivityDateSession") },
			};

			if (IncludeDateLogColumn)
			{
				header.Add("DateLog", ResourceProvider.GetString("LOCGameActivityDateLog"));
			}

			header.Add("PlaytimeSeconds", ResourceProvider.GetString("LOCTimePlayed"));
			header.Add("PlaytimeFormatted", ResourceProvider.GetString("LOCTimePlayed"));
			header.Add("PCName", ResourceProvider.GetString("LOCGameActivityPCName"));
			bool avgHeaders = UseAverageColumnHeaders;
			header.Add("CpuUsage", ResourceProvider.GetString(avgHeaders ? "LOCGameActivityAvgCpu" : "LOCGameActivityCpuUsage"));
			header.Add("RamUsage", ResourceProvider.GetString(avgHeaders ? "LOCGameActivityAvgRam" : "LOCGameActivityRamUsage"));
			header.Add("GpuUsage", ResourceProvider.GetString(avgHeaders ? "LOCGameActivityAvgGpu" : "LOCGameActivityGpuUsage"));
			header.Add("Fps", ResourceProvider.GetString(avgHeaders ? "LOCGameActivityAvgFps" : "LOCGameActivityFps"));

			if (IncludeFrameratePercentileRawColumns)
			{
				header.Add("Fps1PercentLow", ResourceProvider.GetString("LOCGameActivityFps1PercentLow"));
				header.Add("Fps0Point1PercentLow", ResourceProvider.GetString("LOCGameActivityFps0Point1PercentLow"));
			}

			if (IncludeSessionFpsAggregateColumns)
			{
				header.Add("FpsSessionMin", ResourceProvider.GetString("LOCGameActivitySessionFpsMin"));
				header.Add("FpsSessionMax", ResourceProvider.GetString("LOCGameActivitySessionFpsMax"));
				header.Add("FpsSessionMedian", ResourceProvider.GetString("LOCGameActivitySessionFpsMedian"));
				header.Add("FpsSessionStdDev", ResourceProvider.GetString("LOCGameActivitySessionFpsStdDev"));
			}

			if (IncludeFrameratePercentileSummaryColumns)
			{
				header.Add("Fps1PercentLowAvg", ResourceProvider.GetString("LOCGameActivityFps1PercentLowAvg"));
				header.Add("Fps1PercentLowMin", ResourceProvider.GetString("LOCGameActivityFps1PercentLowMin"));
				header.Add("Fps0Point1PercentLowAvg", ResourceProvider.GetString("LOCGameActivityFps0Point1PercentLowAvg"));
				header.Add("Fps0Point1PercentLowMin", ResourceProvider.GetString("LOCGameActivityFps0Point1PercentLowMin"));
			}

			header.Add("CpuTemp", ResourceProvider.GetString(avgHeaders ? "LOCGameActivityAvgCpuT" : "LOCGameActivityCpuTemp"));
			header.Add("GpuTemp", ResourceProvider.GetString(avgHeaders ? "LOCGameActivityAvgGpuT" : "LOCGameActivityGpuTemp"));
			header.Add("CpuPower", ResourceProvider.GetString(avgHeaders ? "LOCGameActivityAvgCpuP" : "LOCGameActivityCpuPower"));
			header.Add("GpuPower", ResourceProvider.GetString(avgHeaders ? "LOCGameActivityAvgGpuP" : "LOCGameActivityGpuPower"));

			return header;
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
				rows.Add(BuildSessionSummaryCsvRow(item, session));
			}

			return rows;
		}

		/// <summary>
		/// Builds one CSV row per session using session-level aggregates from <see cref="GameActivities"/>
		/// (averages, FPS distribution). Does not emit individual <see cref="ActivityDetailsData"/> samples.
		/// </summary>
		protected Dictionary<string, string> BuildSessionSummaryCsvRow(GameActivities game, Activity session)
		{
			DateTime sessionKey = session.DateSession;
			int fpsSessionMin = 0;
			int fpsSessionMax = 0;
			int fpsSessionMedian = 0;
			int fpsSessionStdDev = 0;
			if (IncludeSessionFpsAggregateColumns)
			{
				fpsSessionMin = game.MinFPS(sessionKey);
				fpsSessionMax = game.MaxFPS(sessionKey);
				fpsSessionMedian = game.MedianFPS(sessionKey);
				fpsSessionStdDev = game.StdDevFPS(sessionKey);
			}

			var row = new Dictionary<string, string>
			{
				{ "GameName", game.Name },
				{ "Source", game.Game?.Source?.Name ?? "Playnite" },
				{ "DateSession", FormatCsvUtcDateTime(session.DateSession) },
				{ "PlaytimeSeconds", session.ElapsedSeconds.ToString() },
				{ "PlaytimeFormatted", FormatTimeSpan(TimeSpan.FromSeconds(session.ElapsedSeconds)) },
				{ "PCName", session.Configuration?.Name ?? string.Empty },
				{ "CpuUsage", game.AvgCPU(sessionKey).ToString() },
				{ "RamUsage", game.AvgRAM(sessionKey).ToString() },
				{ "GpuUsage", game.AvgGPU(sessionKey).ToString() },
				{ "Fps", game.AvgFPS(sessionKey).ToString() },
			};

			if (IncludeSessionFpsAggregateColumns)
			{
				row.Add("FpsSessionMin", fpsSessionMin.ToString());
				row.Add("FpsSessionMax", fpsSessionMax.ToString());
				row.Add("FpsSessionMedian", fpsSessionMedian.ToString());
				row.Add("FpsSessionStdDev", fpsSessionStdDev.ToString());
			}

			if (IncludeFrameratePercentileSummaryColumns)
			{
				row.Add("Fps1PercentLowAvg", game.AvgFPS1PercentLow(sessionKey).ToString());
				row.Add("Fps1PercentLowMin", game.MinFPS1PercentLow(sessionKey).ToString());
				row.Add("Fps0Point1PercentLowAvg", game.AvgFPS0Point1PercentLow(sessionKey).ToString());
				row.Add("Fps0Point1PercentLowMin", game.MinFPS0Point1PercentLow(sessionKey).ToString());
			}

			row.Add("CpuTemp", game.AvgCPUT(sessionKey).ToString());
			row.Add("GpuTemp", game.AvgGPUT(sessionKey).ToString());
			row.Add("CpuPower", game.AvgCPUP(sessionKey).ToString());
			row.Add("GpuPower", game.AvgGPUP(sessionKey).ToString());

			return row;
		}

		/// <summary>
		/// Builds CSV rows for one session: one row per <see cref="ActivityDetailsData"/> log sample,
		/// or one summary row when there are no samples (used by <see cref="GameActivityDetailsExport"/> only).
		/// </summary>
		protected List<Dictionary<string, string>> GetRowsForSingleSession(GameActivities item, Activity session)
		{
			var rows = new List<Dictionary<string, string>>();
			if (item == null || session == null)
			{
				return rows;
			}

			List<ActivityDetailsData> sessionDetails = item.GetSessionActivityDetails(session.DateSession);
			DateTime sessionKey = session.DateSession;
			int fpsSessionMin = 0;
			int fpsSessionMax = 0;
			int fpsSessionMedian = 0;
			int fpsSessionStdDev = 0;
			if (IncludeSessionFpsAggregateColumns)
			{
				fpsSessionMin = item.MinFPS(sessionKey);
				fpsSessionMax = item.MaxFPS(sessionKey);
				fpsSessionMedian = item.MedianFPS(sessionKey);
				fpsSessionStdDev = item.StdDevFPS(sessionKey);
			}

			if (sessionDetails != null && sessionDetails.Count > 0)
			{
				foreach (ActivityDetailsData log in sessionDetails)
				{
					rows.Add(BuildActivityCsvRow(
						item,
						session,
						log,
						fpsSessionMin,
						fpsSessionMax,
						fpsSessionMedian,
						fpsSessionStdDev));
				}
			}
			else
			{
				rows.Add(BuildActivityCsvRow(
					item,
					session,
					null,
					fpsSessionMin,
					fpsSessionMax,
					fpsSessionMedian,
					fpsSessionStdDev));
			}

			return rows;
		}

		/// <summary>
		/// Builds one CSV row for a single performance log sample (<see cref="ActivityDetailsData"/>).
		/// Session-level FPS aggregates are repeated on each row when a session has multiple samples.
		/// </summary>
		protected Dictionary<string, string> BuildActivityCsvRow(
			GameActivities game,
			Activity session,
			ActivityDetailsData log,
			int fpsSessionMin,
			int fpsSessionMax,
			int fpsSessionMedian,
			int fpsSessionStdDev)
		{
			var row = new Dictionary<string, string>
			{
				{ "GameName", game.Name },
				{ "Source", game.Game?.Source?.Name ?? "Playnite" },
				{ "DateSession", FormatCsvUtcDateTime(session.DateSession) },
			};

			if (IncludeDateLogColumn)
			{
				row.Add("DateLog", FormatCsvUtcDateTime(log?.Datelog));
			}

			row.Add("PlaytimeSeconds", session.ElapsedSeconds.ToString());
			row.Add("PlaytimeFormatted", FormatTimeSpan(TimeSpan.FromSeconds(session.ElapsedSeconds)));
			row.Add("PCName", session.Configuration?.Name ?? string.Empty);
			row.Add("CpuUsage", log?.CPU.ToString() ?? string.Empty);
			row.Add("RamUsage", log?.RAM.ToString() ?? string.Empty);
			row.Add("GpuUsage", log?.GPU.ToString() ?? string.Empty);
			row.Add("Fps", log?.FPS.ToString() ?? string.Empty);

			if (IncludeFrameratePercentileRawColumns)
			{
				row.Add("Fps1PercentLow", log != null ? log.FPS1PercentLow.ToString() : string.Empty);
				row.Add("Fps0Point1PercentLow", log != null ? log.FPS0Point1PercentLow.ToString() : string.Empty);
			}

			if (IncludeSessionFpsAggregateColumns)
			{
				row.Add("FpsSessionMin", fpsSessionMin.ToString());
				row.Add("FpsSessionMax", fpsSessionMax.ToString());
				row.Add("FpsSessionMedian", fpsSessionMedian.ToString());
				row.Add("FpsSessionStdDev", fpsSessionStdDev.ToString());
			}

			row.Add("CpuTemp", log?.CPUT.ToString() ?? string.Empty);
			row.Add("GpuTemp", log?.GPUT.ToString() ?? string.Empty);
			row.Add("CpuPower", log?.CPUP.ToString() ?? string.Empty);
			row.Add("GpuPower", log?.GPUP.ToString() ?? string.Empty);

			return row;
		}

		protected string FormatTimeSpan(TimeSpan ts)
		{
			return string.Format("{0:00}:{1:00}:{2:00}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
		}
	}
}