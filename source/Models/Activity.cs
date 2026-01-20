using Playnite.SDK;
using Playnite.SDK.Data;
using CommonPluginsShared;
using System;
using System.Collections.Generic;
using GameActivity.Services;

namespace GameActivity.Models
{
	/// <summary>
	/// Represents a gaming activity session with source, platform, configuration, and elapsed time information.
	/// </summary>
	public class Activity : ObservableObject
	{
		// Constants
		private const int NO_CONFIGURATION = -1;

		/// <summary>
		/// Gets the plugin database instance for accessing game activity data.
		/// </summary>
		private static ActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

		// Serialized fields
		private string _gameActionName;

		/// <summary>
		/// Gets or sets the unique identifier of the game source (e.g., Steam, GOG, Epic).
		/// </summary>
		public Guid SourceID { get; set; }

		/// <summary>
		/// Gets or sets the list of platform identifiers associated with this activity.
		/// </summary>
		public List<Guid> PlatformIDs { get; set; }

		/// <summary>
		/// Gets or sets the name of the game action that was executed.
		/// Returns a default localized string if the action name is null or empty.
		/// </summary>
		[SerializationPropertyName("GameActionName")]
		public string GameActionName
		{
			get => string.IsNullOrEmpty(_gameActionName)
				? ResourceProvider.GetString("LOCGameActivityDefaultAction")
				: _gameActionName;
			set => _gameActionName = value;
		}

		/// <summary>
		/// Gets or sets the configuration identifier used for this activity session.
		/// A value of -1 indicates no specific configuration is associated.
		/// </summary>
		public int IdConfiguration { get; set; } = NO_CONFIGURATION;

		/// <summary>
		/// Gets or sets the date and time when this activity session occurred.
		/// Null indicates the session date is not set or unknown.
		/// </summary>
		public DateTime? DateSession { get; set; }

		/// <summary>
		/// Gets or sets the elapsed time of the activity session in seconds.
		/// Defaults to 0 if not explicitly set.
		/// </summary>
		public ulong ElapsedSeconds { get; set; }

		// Computed properties (not serialized)

		/// <summary>
		/// Gets the name of the source or platform associated with this activity.
		/// This property is not serialized and is computed on-demand.
		/// </summary>
		[DontSerialize]
		public string SourceName => PlayniteTools.GetSourceBySourceIdOrPlatformId(SourceID, PlatformIDs);

		/// <summary>
		/// Gets the system configuration associated with this activity.
		/// Returns a default SystemConfiguration if IdConfiguration is invalid or out of range.
		/// This property is not serialized and is computed on-demand.
		/// </summary>
		[DontSerialize]
		public SystemConfiguration Configuration
		{
			get
			{
				// Return default configuration if none is specified
				if (IdConfiguration == NO_CONFIGURATION)
				{
					return new SystemConfiguration();
				}

				List<SystemConfiguration> configurations = PluginDatabase?.LocalSystem?.GetConfigurations();

				// Return default configuration if configurations list is null or index is out of range
				if (configurations == null || IdConfiguration >= configurations.Count)
				{
					return new SystemConfiguration();
				}

				// Return the configuration at the specified index
				return configurations[IdConfiguration];
			}
		}
	}
}