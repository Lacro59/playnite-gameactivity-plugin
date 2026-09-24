using System;

namespace GameActivity.Models
{
    /// <summary>
    /// Aggregation mode for the GameActivity home period charts.
    /// </summary>
    public enum AggregateKind
    {
        /// <summary>Playtime by game (default).</summary>
        Games = 0,

        /// <summary>Playtime by store / source (includes day and week charts).</summary>
        Sources = 1,

        /// <summary>Playtime by genre.</summary>
        Genres = 2,

        /// <summary>Playtime by tag.</summary>
        Tags = 3
    }
}
