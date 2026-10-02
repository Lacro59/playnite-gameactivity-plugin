using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using CommonPluginsControls.LiveChartsCommon;
using CommonPluginsShared.Extensions;
using GameActivity.Models;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;

namespace GameActivity.Services
{
    /// <summary>
    /// Builds LiveCharts pie / colored column series for aggregate period charts (Top N + Others).
    /// Uses per-series mappers so the global XY <see cref="CustomerForTime"/> mapper stays intact.
    /// </summary>
    public static class AggregatePieChartHelper
    {
        /// <summary>Max named slices on the complementary pie before folding into Others.</summary>
        public const int PieChartTopCount = 10;

        private static readonly PieMapper<CustomerForTime> PieMapper = Mappers.Pie<CustomerForTime>()
            .Value(value => value.Values);

        /// <summary>Distinct fills when store colors / default brush are missing (tooltip swatch needs Series.Fill).</summary>
        private static readonly Brush[] DefaultPiePalette = CreateDefaultPiePalette();

        private static Brush[] CreateDefaultPiePalette()
        {
            Color[] colors =
            {
                Color.FromRgb(0x4E, 0x79, 0xA7),
                Color.FromRgb(0xF2, 0x8E, 0x2B),
                Color.FromRgb(0xE1, 0x57, 0x59),
                Color.FromRgb(0x76, 0xB7, 0xB2),
                Color.FromRgb(0x59, 0xA1, 0x4F),
                Color.FromRgb(0xED, 0xC9, 0x48),
                Color.FromRgb(0xB0, 0x7A, 0xA1),
                Color.FromRgb(0xFF, 0x9D, 0xA7),
                Color.FromRgb(0x9C, 0x75, 0x5F),
                Color.FromRgb(0xBA, 0xB0, 0xAC),
                Color.FromRgb(0x86, 0xBC, 0xB6),
                Color.FromRgb(0xD3, 0x72, 0x95)
            };

            Brush[] brushes = new Brush[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                SolidColorBrush brush = new SolidColorBrush(colors[i]);
                brush.Freeze();
                brushes[i] = brush;
            }

            return brushes;
        }

        /// <summary>
        /// Keeps the top <paramref name="topN"/> entries by playtime and folds the rest into an Others bucket.
        /// </summary>
        /// <param name="source">Playtime seconds keyed by display name.</param>
        /// <param name="topN">Max named entries to keep.</param>
        /// <param name="othersLabel">Localized Others label.</param>
        /// <returns>Ordered pairs (top then optional Others); empty when <paramref name="source"/> is null or empty.</returns>
        public static List<KeyValuePair<string, ulong>> ReduceToTopNWithOthers(
            Dictionary<string, ulong> source,
            int topN,
            string othersLabel)
        {
            if (source == null || source.Count == 0)
            {
                return new List<KeyValuePair<string, ulong>>();
            }

            List<KeyValuePair<string, ulong>> ordered = source
                .OrderByDescending(x => x.Value)
                .ToList();

            if (ordered.Count <= topN)
            {
                return ordered;
            }

            List<KeyValuePair<string, ulong>> result = new List<KeyValuePair<string, ulong>>(topN + 1);
            for (int i = 0; i < topN; i++)
            {
                result.Add(ordered[i]);
            }

            ulong othersTotal = 0;
            for (int i = topN; i < ordered.Count; i++)
            {
                othersTotal += ordered[i].Value;
            }

            if (othersTotal > 0)
            {
                result.Add(new KeyValuePair<string, ulong>(othersLabel ?? string.Empty, othersTotal));
            }

            return result;
        }

        /// <summary>
        /// Reduces raw playtime pairs to pie Top N + Others.
        /// </summary>
        /// <param name="source">Playtime seconds keyed by display name.</param>
        /// <param name="othersLabel">Localized Others label.</param>
        public static List<KeyValuePair<string, ulong>> ReduceForPie(
            Dictionary<string, ulong> source,
            string othersLabel)
        {
            return ReduceToTopNWithOthers(source, PieChartTopCount, othersLabel);
        }

        /// <summary>
        /// Maps reduced pairs to tooltip-ready <see cref="CustomerForTime"/> points (Name + Values only).
        /// </summary>
        /// <param name="pairs">Reduced name → seconds pairs.</param>
        public static List<CustomerForTime> ToCustomerForTime(IList<KeyValuePair<string, ulong>> pairs)
        {
            if (pairs == null || pairs.Count == 0)
            {
                return new List<CustomerForTime>();
            }

            List<CustomerForTime> items = new List<CustomerForTime>(pairs.Count);
            for (int i = 0; i < pairs.Count; i++)
            {
                KeyValuePair<string, ulong> pair = pairs[i];
                items.Add(new CustomerForTime
                {
                    Name = pair.Key,
                    Values = (long)pair.Value
                });
            }

            return items;
        }

        /// <summary>
        /// Builds a name → brush map so pie slices and matching bars share colors.
        /// Primary items (usually pie) are colored first; extras (bar-only) get the next palette slots.
        /// </summary>
        /// <param name="primaryItems">Items that define the shared palette order (pie).</param>
        /// <param name="extraItems">Additional bar items not already in <paramref name="primaryItems"/>.</param>
        /// <param name="defaultFill">Optional fallback before palette.</param>
        /// <param name="storeColors">Optional store color list (Sources).</param>
        /// <param name="useStoreColors">When true, resolve Fill from <paramref name="storeColors"/> by name.</param>
        public static Dictionary<string, Brush> BuildNameBrushMap(
            IList<CustomerForTime> primaryItems,
            IList<CustomerForTime> extraItems,
            Brush defaultFill,
            IList<StoreColor> storeColors,
            bool useStoreColors)
        {
            Dictionary<string, Brush> map = new Dictionary<string, Brush>(StringComparer.Ordinal);
            int paletteIndex = 0;
            AppendBrushMap(map, primaryItems, ref paletteIndex, defaultFill, storeColors, useStoreColors);
            AppendBrushMap(map, extraItems, ref paletteIndex, defaultFill, storeColors, useStoreColors);
            return map;
        }

        private static void AppendBrushMap(
            Dictionary<string, Brush> map,
            IList<CustomerForTime> items,
            ref int paletteIndex,
            Brush defaultFill,
            IList<StoreColor> storeColors,
            bool useStoreColors)
        {
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                CustomerForTime item = items[i];
                if (item == null || string.IsNullOrEmpty(item.Name) || map.ContainsKey(item.Name))
                {
                    continue;
                }

                Brush fill = ResolveSliceFill(item.Name, ref paletteIndex, defaultFill, storeColors, useStoreColors);
                map[item.Name] = fill;
            }
        }

        private static Brush ResolveSliceFill(
            string name,
            ref int paletteIndex,
            Brush defaultFill,
            IList<StoreColor> storeColors,
            bool useStoreColors)
        {
            Brush fill = null;
            if (useStoreColors)
            {
                fill = ResolveStoreFill(name, storeColors);
            }

            if (fill == null)
            {
                fill = defaultFill;
            }

            if (fill == null)
            {
                fill = DefaultPiePalette[paletteIndex % DefaultPiePalette.Length];
                paletteIndex++;
            }

            return fill;
        }

        /// <summary>
        /// Builds one <see cref="PieSeries"/> per slice using a shared name → brush map.
        /// </summary>
        /// <param name="pieItems">Slice points (typically Top 10 + Others).</param>
        /// <param name="brushMap">Shared colors with the column chart.</param>
        /// <param name="fallbackFill">Used when a name is missing from the map.</param>
        public static SeriesCollection BuildPieSeries(
            IList<CustomerForTime> pieItems,
            IDictionary<string, Brush> brushMap,
            Brush fallbackFill)
        {
            SeriesCollection series = new SeriesCollection();
            if (pieItems == null || pieItems.Count == 0)
            {
                return series;
            }

            for (int i = 0; i < pieItems.Count; i++)
            {
                CustomerForTime item = pieItems[i];
                if (item == null || item.Values <= 0)
                {
                    continue;
                }

                Brush fill = fallbackFill;
                Brush mapped;
                if (brushMap != null
                    && !string.IsNullOrEmpty(item.Name)
                    && brushMap.TryGetValue(item.Name, out mapped)
                    && mapped != null)
                {
                    fill = mapped;
                }

                series.Add(new PieSeries
                {
                    Title = item.Name ?? string.Empty,
                    Values = new ChartValues<CustomerForTime> { item },
                    Configuration = PieMapper,
                    DataLabels = false,
                    Fill = fill
                });
            }

            return series;
        }

        /// <summary>
        /// Builds a single <see cref="ColumnSeries"/> with per-bar Fill from the shared brush map.
        /// </summary>
        /// <param name="items">Column points (same order as axis labels).</param>
        /// <param name="brushMap">Shared colors with the pie chart.</param>
        /// <param name="fallbackFill">Used when a name is missing from the map.</param>
        public static SeriesCollection BuildColoredColumnSeries(
            IList<CustomerForTime> items,
            IDictionary<string, Brush> brushMap,
            Brush fallbackFill)
        {
            SeriesCollection series = new SeriesCollection();
            ChartValues<CustomerForTime> values = new ChartValues<CustomerForTime>();
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    values.Add(items[i]);
                }
            }

            IDictionary<string, Brush> map = brushMap;
            Brush fallback = fallbackFill;
            CartesianMapper<CustomerForTime> mapper = Mappers.Xy<CustomerForTime>()
                .X((value, index) => index)
                .Y(value => value.Values)
                .Fill(value =>
                {
                    if (value != null
                        && !string.IsNullOrEmpty(value.Name)
                        && map != null)
                    {
                        Brush mapped;
                        if (map.TryGetValue(value.Name, out mapped) && mapped != null)
                        {
                            return mapped;
                        }
                    }

                    return fallback;
                });

            series.Add(new ColumnSeries
            {
                Title = string.Empty,
                Values = values,
                Configuration = mapper,
                DataLabels = false
            });

            return series;
        }

        /// <summary>
        /// Resolves a store brush by name (case-insensitive contains), or null when missing.
        /// </summary>
        /// <param name="sourceName">Store / source display name.</param>
        /// <param name="storeColors">Configured store colors.</param>
        public static Brush ResolveStoreFill(string sourceName, IList<StoreColor> storeColors)
        {
            if (string.IsNullOrEmpty(sourceName) || storeColors == null || storeColors.Count == 0)
            {
                return null;
            }

            StoreColor match = storeColors
                .Where(x => x != null
                    && !string.IsNullOrEmpty(x.Name)
                    && x.Name.Contains(sourceName, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();

            return match?.Fill;
        }
    }
}
