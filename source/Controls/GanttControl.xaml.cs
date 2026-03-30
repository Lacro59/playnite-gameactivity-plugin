using CommonPlayniteShared.Converters;
using CommonPluginsShared;
using CommonPluginsShared.Converters;
using GameActivity.Views;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace GameActivity.Controls
{
    /// <summary>
    /// Interaction logic for GanttControl.xaml.
    /// Renders a compact horizontal Gantt bar where each column represents one day.
    /// Populated entirely in code-behind because column count is dynamic.
    /// </summary>
    public partial class GanttControl : UserControl
    {
        internal static ILogger Logger => LogManager.GetLogger();

        private DataContextGanttControl DataContextGanttControl { get; set; } = new DataContextGanttControl();

        #region Properties

        // -----------------------------------------------------------------
        // ColorItem — derived deterministically from DataName so the same
        // game always renders with the same colour across sessions.
        // Registered as a proper DependencyProperty so external callers can
        // override it if needed (e.g. from a theme binding).
        // -----------------------------------------------------------------

        public static readonly DependencyProperty ColorItemProperty = DependencyProperty.Register(
            nameof(ColorItem),
            typeof(SolidColorBrush),
            typeof(GanttControl),
            new FrameworkPropertyMetadata(Brushes.Transparent));

        /// <summary>
        /// Fill colour used for activity cells.
        /// Defaults to a deterministic hue derived from <see cref="DataName"/>.
        /// Can be overridden externally via data-binding.
        /// </summary>
        public SolidColorBrush ColorItem
        {
            get => (SolidColorBrush)GetValue(ColorItemProperty);
            set => SetValue(ColorItemProperty, value);
        }

        // -----------------------------------------------------------------
        // DataName — display name shown in the tooltip title row.
        // -----------------------------------------------------------------

        public string DataName
        {
            get => (string)GetValue(DataNameProperty);
            set => SetValue(DataNameProperty, value);
        }

        public static readonly DependencyProperty DataNameProperty = DependencyProperty.Register(
            nameof(DataName),
            typeof(string),
            typeof(GanttControl),
            new FrameworkPropertyMetadata(string.Empty, ControlPropertyChangedCallback));

        // -----------------------------------------------------------------
        // ColumnCount — number of day-columns to render (default 10).
        // -----------------------------------------------------------------

        public int ColumnCount
        {
            get => (int)GetValue(ColumnCountProperty);
            set => SetValue(ColumnCountProperty, value);
        }

        public static readonly DependencyProperty ColumnCountProperty = DependencyProperty.Register(
            nameof(ColumnCount),
            typeof(int),
            typeof(GanttControl),
            new FrameworkPropertyMetadata(10, ControlPropertyChangedCallback));

        // -----------------------------------------------------------------
        // LastDate — the rightmost day displayed in the Gantt bar.
        // -----------------------------------------------------------------

        public DateTime LastDate
        {
            get => (DateTime)GetValue(LastDateProperty);
            set => SetValue(LastDateProperty, value);
        }

        public static readonly DependencyProperty LastDateProperty = DependencyProperty.Register(
            nameof(LastDate),
            typeof(DateTime),
            typeof(GanttControl),
            new FrameworkPropertyMetadata(DateTime.Now, ControlPropertyChangedCallback));

        // -----------------------------------------------------------------
        // Values — list of activity entries (date + playtime) to display.
        // -----------------------------------------------------------------

        public List<GanttValue> Values
        {
            get => (List<GanttValue>)GetValue(ValuesProperty);
            set => SetValue(ValuesProperty, value);
        }

        public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
            nameof(Values),
            typeof(List<GanttValue>),
            typeof(GanttControl),
            new FrameworkPropertyMetadata(new List<GanttValue>(), ControlPropertyChangedCallback));

        // -----------------------------------------------------------------
        // OnlyDate — when true the control renders date labels instead of
        // activity blocks (used as the axis row beneath the Gantt grid).
        // -----------------------------------------------------------------

        public bool OnlyDate
        {
            get => (bool)GetValue(OnlyDateProperty);
            set => SetValue(OnlyDateProperty, value);
        }

        public static readonly DependencyProperty OnlyDateProperty = DependencyProperty.Register(
            nameof(OnlyDate),
            typeof(bool),
            typeof(GanttControl),
            new FrameworkPropertyMetadata(false, ControlPropertyChangedCallback));

        // -----------------------------------------------------------------
        // Shared callback — dispatches property changes to the right methods.
        // -----------------------------------------------------------------

        private static void ControlPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is GanttControl obj) || e.NewValue == e.OldValue)
            {
                return;
            }

            switch (e.Property.Name)
            {
                case nameof(DataName):
                    // Regenerate the deterministic colour whenever the game name changes.
                    obj.ColorItem = BuildColorFromName((string)e.NewValue);
                    break;

                case nameof(ColumnCount):
                    obj.DefineColumn();
                    obj.SetData();
                    obj.SetDataDate();
                    break;

                case nameof(LastDate):
                case nameof(Values):
                    obj.SetData();
                    obj.SetDataDate();
                    break;

                case nameof(OnlyDate):
                    obj.SetDataDate();
                    break;
            }
        }

        #endregion

        // -----------------------------------------------------------------
        // Constructor
        // -----------------------------------------------------------------

        public GanttControl()
        {
            InitializeComponent();
            DataContext = DataContextGanttControl;

            PART_Gantt.Children.Clear();

            // Initial colour — will be updated when DataName is set.
            ColorItem = BuildColorFromName(string.Empty);

            DefineColumn();
        }

        // -----------------------------------------------------------------
        // Colour generation
        // -----------------------------------------------------------------

        /// <summary>
        /// Derives a deterministic <see cref="SolidColorBrush"/> from a game name by hashing
        /// it with MD5 and mapping the first three bytes to R/G/B channels.
        /// The same name always produces the same colour; different names produce
        /// visually distinct colours spread across the hue wheel.
        /// </summary>
        /// <param name="name">Game name (may be null or empty).</param>
        /// <returns>A fully opaque brush whose colour is stable for the given name.</returns>
        private static SolidColorBrush BuildColorFromName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                // Fallback: theme accent colour via a neutral mid-grey.
                return new SolidColorBrush(Color.FromRgb(120, 140, 200));
            }

            // MD5 is used purely for colour distribution — not for security.
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(name));
                // Clamp minimum brightness so cells remain visible on dark backgrounds.
                // Explicit int cast resolves the Math.Max(int, int) overload unambiguously (CS0121).
                byte r = (byte)Math.Max(80, (int)hash[0]);
                byte g = (byte)Math.Max(80, (int)hash[1]);
                byte b = (byte)Math.Max(80, (int)hash[2]);
                return new SolidColorBrush(Color.FromRgb(r, g, b));
            }
        }

        // -----------------------------------------------------------------
        // Column layout
        // -----------------------------------------------------------------

        /// <summary>Rebuilds the <see cref="PART_Gantt"/> column definitions from <see cref="ColumnCount"/>.</summary>
        private void DefineColumn()
        {
            PART_Gantt.ColumnDefinitions.Clear();

            // ColumnCount + 1 columns: indices 0..ColumnCount inclusive.
            for (int idx = 0; idx <= ColumnCount; idx++)
            {
                try
                {
                    PART_Gantt.ColumnDefinitions.Add(new ColumnDefinition
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
        }

        // -----------------------------------------------------------------
        // Activity data rendering
        // -----------------------------------------------------------------

        /// <summary>
        /// Populates <see cref="PART_Gantt"/> with coloured <see cref="Border"/> cells
        /// for each day that has a matching entry in <see cref="Values"/>.
        /// Each cell carries a themed tooltip showing the game name, date, and playtime.
        /// </summary>
        private void SetData()
        {
            if (Values == null || Values.Count == 0)
            {
                return;
            }

            PART_Gantt.Children.Clear();

            var localDateConverter = new LocalDateConverter();
            var playTimeConverter = new PlayTimeToStringConverterWithZero();

            for (int idx = ColumnCount; idx >= 0; idx--)
            {
                try
                {
                    DateTime dt = LastDate.AddDays(idx * -1);
                    string dateKey = dt.ToString("yyyy-MM-dd");

                    GanttValue found = Values.Find(x =>
                        x.PlayDate.ToString("yyyy-MM-dd") == dateKey);

                    if (found == null)
                    {
                        continue;
                    }

                    // --- Activity cell ---
                    Border cell = new Border
                    {
                        Background = ColorItem,
                        BorderThickness = (Thickness)ResourceProvider.GetResource("ControlBorderThickness"),
                        BorderBrush = (Brush)ResourceProvider.GetResource("PopupBorderBrush"),
                        CornerRadius = (CornerRadius)ResourceProvider.GetResource("ControlCornerRadius"),
                        ToolTip = BuildTooltip(found, localDateConverter, playTimeConverter)
                    };

                    Grid.SetColumn(cell, ColumnCount - idx);
                    PART_Gantt.Children.Add(cell);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
        }

        /// <summary>
        /// Builds a themed tooltip <see cref="Border"/> for an activity cell.
        /// The tooltip uses <c>PopupBackgroundBrush</c> and <c>PopupBorderBrush</c>
        /// from the active theme so it integrates naturally with Playnite's visual style.
        /// </summary>
        /// <param name="value">The activity entry whose data is displayed.</param>
        /// <param name="dateConverter">Converter used to format the play date.</param>
        /// <param name="timeConverter">Converter used to format the playtime.</param>
        /// <returns>A <see cref="Border"/> suitable for use as a WPF ToolTip.</returns>
        private Border BuildTooltip(
            GanttValue value,
            LocalDateConverter dateConverter,
            PlayTimeToStringConverterWithZero timeConverter)
        {
            // Outer container — themed background + rounded border.
            Border tooltipBorder = new Border
            {
                Background = (Brush)ResourceProvider.GetResource("PopupBackgroundBrush"),
                BorderBrush = (Brush)ResourceProvider.GetResource("PopupBorderBrush"),
                BorderThickness = (Thickness)ResourceProvider.GetResource("PopupBorderThickness"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6)
            };

            StackPanel panel = new StackPanel();

            // --- Game name row (bold, visible only when DataName is not empty) ---
            TextBlock nameBlock = new TextBlock
            {
                Style = (Style)ResourceProvider.GetResource("BaseTextBlockStyle"),
                FontWeight = FontWeights.SemiBold,
                FontSize = (double)ResourceProvider.GetResource("FontSize"),
                Foreground = (Brush)ResourceProvider.GetResource("TextBrush"),
                Margin = new Thickness(0, 0, 0, 4)
            };

            // Bind Text to DataName so the block stays in sync if the property changes.
            Binding textBinding = new Binding
            {
                Source = DataName,
                Mode = BindingMode.OneWay
            };
            nameBlock.SetBinding(TextBlock.TextProperty, textBinding);

            // Hide the name row when DataName is empty to avoid an empty line.
            Binding visibilityBinding = new Binding
            {
                Converter = new StringNullOrEmptyToVisibilityConverter(),
                Source = DataName,
                Mode = BindingMode.OneWay
            };
            nameBlock.SetBinding(VisibilityProperty, visibilityBinding);

            panel.Children.Add(nameBlock);

            // --- Date + playtime row ---
            string formattedDate = dateConverter.Convert(value.PlayDate, null, null, CultureInfo.CurrentCulture)?.ToString() ?? string.Empty;
            string formattedPlayTime = timeConverter.Convert(value.PlayTime, null, null, CultureInfo.CurrentCulture)?.ToString() ?? string.Empty;

            TextBlock detailBlock = new TextBlock
            {
                Style = (Style)ResourceProvider.GetResource("BaseTextBlockStyle"),
                FontSize = (double)ResourceProvider.GetResource("FontSizeSmall"),
                Foreground = (Brush)ResourceProvider.GetResource("TextBrushDarker"),
                Text = formattedDate + "  —  " + formattedPlayTime
            };

            panel.Children.Add(detailBlock);

            tooltipBorder.Child = panel;
            return tooltipBorder;
        }

        // -----------------------------------------------------------------
        // Date axis rendering
        // -----------------------------------------------------------------

        /// <summary>
        /// When <see cref="OnlyDate"/> is <c>true</c>, replaces all cells with
        /// rotated date-label <see cref="TextBlock"/> elements (270° — vertical descending).
        /// </summary>
        private void SetDataDate()
        {
            if (!OnlyDate)
            {
                return;
            }

            PART_Gantt.Children.Clear();

            var localDateConverter = new LocalDateConverter();

            for (int idx = ColumnCount; idx >= 0; idx--)
            {
                try
                {
                    DateTime dt = LastDate.AddDays(idx * -1);

                    Grid cell = new Grid();
                    Grid.SetColumn(cell, ColumnCount - idx);

                    TextBlock label = new TextBlock
                    {
                        Style = (Style)ResourceProvider.GetResource("BaseTextBlockStyle"),
                        FontSize = (double)ResourceProvider.GetResource("FontSizeSmall"),
                        Foreground = (Brush)ResourceProvider.GetResource("TextBrushDarker"),
                        Text = localDateConverter.Convert(dt, null, null, CultureInfo.CurrentCulture)?.ToString() ?? string.Empty,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        // Rotate 270° — vertical, top-to-bottom reading direction.
                        LayoutTransform = new RotateTransform { Angle = 270 }
                    };

                    cell.Children.Add(label);
                    PART_Gantt.Children.Add(cell);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
        }
    }


    /// <summary>
    /// DataContext for <see cref="GanttControl"/>.
    /// Currently empty — reserved for future MVVM bindings (e.g. visibility toggles,
    /// animated state) without requiring a code-behind change.
    /// </summary>
    public class DataContextGanttControl : ObservableObject
    {
    }
}