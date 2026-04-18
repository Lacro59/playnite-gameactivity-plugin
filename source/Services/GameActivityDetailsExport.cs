using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GameActivity.Models;
using Playnite.SDK;

namespace GameActivity.Services
{
	/// <summary>
	/// CSV export of performance detail samples (<see cref="ActivityDetailsData"/>) for a single session.
	/// </summary>
	public class GameActivityDetailsExport : GameActivityExport
	{
		private DateTime _sessionDateUtc;

		/// <inheritdoc />
		/// Session-level FPS aggregates are not offered on per-sample detail export (redundant with the Fps column).
		protected override bool IncludeSessionFpsAggregateColumns
		{
			get { return false; }
		}

		/// <inheritdoc />
		protected override bool IncludeDateLogColumn
		{
			get { return true; }
		}

		/// <inheritdoc />
		protected override bool IncludeFrameratePercentileSummaryColumns
		{
			get { return false; }
		}

		/// <inheritdoc />
		protected override bool IncludeFrameratePercentileRawColumns
		{
			get { return true; }
		}

		/// <inheritdoc />
		protected override bool UseAverageColumnHeaders
		{
			get { return false; }
		}

		/// <summary>
		/// Opens the export dialog and writes CSV rows for the session identified by <paramref name="sessionDateSession"/>.
		/// </summary>
		/// <param name="pluginName">Plugin display name (dialog title / logging).</param>
		/// <param name="activities">Game activity aggregate containing the session.</param>
		/// <param name="sessionDateSession">Session start instant (typically <see cref="Activity.DateSession"/>).</param>
		/// <returns>True when the user completed export successfully.</returns>
		public bool ExportSessionDetailsToCsv(string pluginName, GameActivities activities, DateTime sessionDateSession)
		{
			_sessionDateUtc = sessionDateSession.ToUniversalTime();
			string suggestedStem = BuildSuggestedExportFileStem(pluginName, activities, sessionDateSession);
			return ExportToCsv(pluginName, new List<GameActivities> { activities }, null, suggestedStem);
		}

		/// <summary>
		/// Builds a descriptive default CSV file stem: plugin, export kind, game name, session start (local).
		/// </summary>
		private static string BuildSuggestedExportFileStem(string pluginName, GameActivities activities, DateTime sessionDateSession)
		{
			string tag = ResourceProvider.GetString("LOCGameActivitySessionDetailsExportFileTag");
			if (string.IsNullOrWhiteSpace(tag))
			{
				tag = "Session-performance-log";
			}

			string pluginPart = SanitizeFileSegment(pluginName, 40);
			string tagPart = SanitizeFileSegment(tag, 48);
			string gamePart = SanitizeFileSegment(activities != null ? activities.Name : null, 80);
			string datePart = sessionDateSession.ToLocalTime().ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);

			return string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}_{3}", pluginPart, tagPart, gamePart, datePart);
		}

		private static string SanitizeFileSegment(string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value))
			{
				return "Game";
			}

			char[] invalid = Path.GetInvalidFileNameChars();
			var builder = new StringBuilder(Math.Min(value.Length, maxLength));
			for (int i = 0; i < value.Length && builder.Length < maxLength; i++)
			{
				char c = value[i];
				if (Array.IndexOf(invalid, c) >= 0)
				{
					c = '_';
				}

				builder.Append(c);
			}

			string result = builder.ToString().Trim();
			if (result.Length == 0)
			{
				return "Game";
			}

			return result;
		}

		protected override IEnumerable<Dictionary<string, string>> GetRows(GameActivities item)
		{
			var rows = new List<Dictionary<string, string>>();
			if (item?.Items == null)
			{
				return rows;
			}

			Activity session = ResolveSession(item, _sessionDateUtc);
			if (session == null)
			{
				return rows;
			}

			return GetRowsForSingleSession(item, session);
		}

		private static Activity ResolveSession(GameActivities item, DateTime sessionUtc)
		{
			string key = sessionUtc.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
			foreach (Activity candidate in item.Items)
			{
				if (candidate.DateSession.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) == key)
				{
					return candidate;
				}
			}

			return null;
		}
	}
}
