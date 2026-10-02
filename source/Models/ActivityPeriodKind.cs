using System;

namespace GameActivity.Models
{
    /// <summary>
    /// Period granularity for the GameActivity home view.
    /// Selects how the period window is sized and how Prev/Next shifts it.
    /// Free date ranges are out of scope for v1.
    /// </summary>
    public enum ActivityPeriodKind
    {
        /// <summary>Seven-day window ending at the period end date; Prev/Next shifts by 7 days.</summary>
        Last7Days = 0,

        /// <summary>A single calendar month; Prev/Next shifts by one month.</summary>
        Month = 1,

        /// <summary>Three calendar months ending at the anchor month; Prev/Next shifts by one month.</summary>
        Last3Months = 2,

        /// <summary>A full calendar year; Prev/Next shifts by one year.</summary>
        Year = 3
    }
}
