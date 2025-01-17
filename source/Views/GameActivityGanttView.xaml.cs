using CommonPluginsShared;
using CommonPluginsShared.Converters;
using GameActivity.Controls;
using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GameActivity.Views
{
    /// <summary>
    /// Logique d'interaction pour GameActivityGanttView.xaml
    /// </summary>
    public partial class GameActivityGanttView : UserControl
    {
        private ActivityDatabase PluginDatabase => GameActivity.PluginDatabase;
        private DataContextGanttView DataContextGanttView { get; set; } = new DataContextGanttView();

        private GanttControl GanttControl { get; set; }


        public GameActivityGanttView()
        {
            InitializeComponent();
            DataContext = DataContextGanttView;

            GetData();
            SetPeriod();
        }


        private void SetPeriod()
        {
            LocalDateConverter localDateConverter = new LocalDateConverter();
            DateTime dtStart = DataContextGanttView.LastDate.AddDays(DataContextGanttView.ColumnCount * - 1);
            PART_Period.Content = localDateConverter.Convert(dtStart, null, null, CultureInfo.CurrentCulture)
                + " - " + localDateConverter.Convert(DataContextGanttView.LastDate, null, null, CultureInfo.CurrentCulture);

            if (DataContextGanttView?.GanttDatas != null)
            {
                foreach (GanttData ganttData in DataContextGanttView.GanttDatas)
                {
                    ganttData.PlaytimeInPerdiod = 0;
                    ganttData.DateTimes
                        .Where(x => x.PlayDate >= new DateTime(dtStart.Year, dtStart.Month, dtStart.Day, 0, 0, 0, DateTimeKind.Local) && x.PlayDate <= new DateTime(DataContextGanttView.LastDate.Year, DataContextGanttView.LastDate.Month, DataContextGanttView.LastDate.Day, 23, 59, 59, DateTimeKind.Local))
                        ?.ForEach(x =>
                        {
                            ganttData.PlaytimeInPerdiod += x.PlayTime;
                        });
                }
            }
        }


        private void GetData()
        {
            List<GanttData> ganttDatas = new List<GanttData>();
            foreach (GameActivities gameActivities in PluginDatabase.Database.Where(x => x.LastActivity != null))
            {
                try
                {
                    GanttData ganttData = new GanttData
                    {
                        Id = gameActivities.Id,
                        Name = gameActivities.Name,
                        Icon = gameActivities.Icon.IsNullOrEmpty() ? gameActivities.Icon : API.Instance.Database.GetFullFilePath(gameActivities.Icon),
                        LastActivity = (DateTime)gameActivities.LastActivity,
                        Playtime = gameActivities.Game.Playtime
                    };

                    List<GanttValue> data = gameActivities.Items.Select(x => new GanttValue { PlayDate = (DateTime)x.DateSession?.ToLocalTime(), PlayTime = x.ElapsedSeconds }).ToList();
                    List<GanttValue> dataFinal = new List<GanttValue>();

                    data.ForEach(x =>
                    {
                        if (dataFinal.Find(y => y.PlayDate.ToString("yyyy-MM-dd") == x.PlayDate.ToString("yyyy-MM-dd")) != null)
                        {
                            dataFinal.Find(y => y.PlayDate.ToString("yyyy-MM-dd") == x.PlayDate.ToString("yyyy-MM-dd")).PlayTime += x.PlayTime;
                        }
                        else
                        {
                            dataFinal.Add(new GanttValue { PlayDate = x.PlayDate, PlayTime = x.PlayTime });
                        }
                    });

                    ganttData.DateTimes = dataFinal;

                    ganttDatas.Add(ganttData);
                }
                catch(Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }

            DataContextGanttView.GanttDatas = ganttDatas;
        }


        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DatePicker control = sender as DatePicker;
            DateTime dateNew = (DateTime)control.SelectedDate;
            DataContextGanttView.LastDate = dateNew;
            SetPeriod();

            if (GanttControl != null)
            {
                GanttControl.LastDate = DataContextGanttView.LastDate;
            }
        }


        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (GanttControl == null)
            {
                GanttControl = new GanttControl
                {
                    OnlyDate = true,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Height = 80
                };

                //Binding bindingColumnCount = new Binding();
                //bindingColumnCount.Source = (int)PART_ColumnCount.Value;
                //bindingColumnCount.Mode = BindingMode.OneWay;
                //ganttControl.SetBinding(GanttControl.ColumnCountProperty, bindingColumnCount);
                GanttControl.ColumnCount = (int)PART_ColumnCount.Value;

                //Binding bindingLastDate = new Binding();
                //bindingLastDate.Source = dataContextGanttView.LastDate;
                //bindingLastDate.Mode = BindingMode.OneWay;
                //ganttControl.SetBinding(GanttControl.LastDateProperty, bindingLastDate);
                GanttControl.LastDate = DataContextGanttView.LastDate;

                //Binding bindingWidth = new Binding();
                //bindingWidth.Source = dataContextGanttView.HeaderWidth - 10;
                //bindingWidth.Mode = BindingMode.OneWay;
                //ganttControl.SetBinding(GanttControl.WidthProperty, bindingWidth);
                GanttControl.Width = DataContextGanttView.HeaderWidth - 10;

                PART_GanttHeader.Content = GanttControl;
            }
        }

        private void PART_ColumnCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetPeriod();

            if (GanttControl != null)
            {
                GanttControl.ColumnCount = (int)PART_ColumnCount.Value;
            }
        }

        private void PART_GanttHeader_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (GanttControl != null)
            {
                GanttControl.Width = DataContextGanttView.HeaderWidth - 10 >= 0 ? DataContextGanttView.HeaderWidth - 10 : 0;
            }
        }
    }


    public class DataContextGanttView : ObservableObject
    {
        private List<GanttData> _ganttDatas;
        public List<GanttData> GanttDatas { get => _ganttDatas; set => SetValue(ref _ganttDatas, value); }

        private int _columnCount = 30;
        public int ColumnCount { get => _columnCount; set => SetValue(ref _columnCount, value); }

        private double _headerWidth = 690;
        public double HeaderWidth { get => _headerWidth; set => SetValue(ref _headerWidth, value); }

        private DateTime _lastDate = DateTime.Now;
        public DateTime LastDate { get => _lastDate; set => SetValue(ref _lastDate, value); }
    }


    public class GanttData: ObservableObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public DateTime LastActivity { get; set; }
        public ulong Playtime { get; set; }

        private ulong _playtimeInPerdiod;
        public ulong PlaytimeInPerdiod { get => _playtimeInPerdiod; set => SetValue(ref _playtimeInPerdiod, value); }

        public List<GanttValue> DateTimes { get; set; }


        public RelayCommand<Guid> GoToGame => Commands.GoToGame;

        public bool GameExist => API.Instance.Database.Games.Get(Id) != null;
    }

    public class GanttValue : ObservableObject
    {
        public DateTime PlayDate { get; set; }
        public ulong PlayTime { get; set; }
    }
}
