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
        private DataContextGanttView dataContextGanttView { get; set; } = new DataContextGanttView();

        private GanttControl ganttControl { get; set; }


        public GameActivityGanttView()
        {
            InitializeComponent();
            this.DataContext = dataContextGanttView;

            SetPeriod();
            GetData();
        }


        private void SetPeriod()
        {
            LocalDateConverter localDateConverter = new LocalDateConverter();
            DateTime dtStart = dataContextGanttView.LastDate.AddDays(dataContextGanttView.ColumnCount * - 1);
            PART_Period.Content = localDateConverter.Convert(dtStart, null, null, CultureInfo.CurrentCulture)
                + " - " + localDateConverter.Convert(dataContextGanttView.LastDate, null, null, CultureInfo.CurrentCulture);
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
                        Icon = gameActivities.Icon.IsNullOrEmpty() ? gameActivities.Icon : PluginDatabase.PlayniteApi.Database.GetFullFilePath(gameActivities.Icon),
                        LastActivity = (DateTime)gameActivities.LastActivity
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

            dataContextGanttView.GanttDatas = ganttDatas;
        }


        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DatePicker control = sender as DatePicker;
            DateTime dateNew = (DateTime)control.SelectedDate;
            dataContextGanttView.LastDate = dateNew;
            SetPeriod();

            if (ganttControl != null)
            {
                ganttControl.LastDate = dataContextGanttView.LastDate;
            }
        }


        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (ganttControl == null)
            {
                ganttControl = new GanttControl
                {
                    OnlyDate = true,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Height = 80
                };

                //Binding bindingColumnCount = new Binding();
                //bindingColumnCount.Source = (int)PART_ColumnCount.Value;
                //bindingColumnCount.Mode = BindingMode.OneWay;
                //ganttControl.SetBinding(GanttControl.ColumnCountProperty, bindingColumnCount);
                ganttControl.ColumnCount = (int)PART_ColumnCount.Value;

                //Binding bindingLastDate = new Binding();
                //bindingLastDate.Source = dataContextGanttView.LastDate;
                //bindingLastDate.Mode = BindingMode.OneWay;
                //ganttControl.SetBinding(GanttControl.LastDateProperty, bindingLastDate);
                ganttControl.LastDate = dataContextGanttView.LastDate;

                //Binding bindingWidth = new Binding();
                //bindingWidth.Source = dataContextGanttView.HeaderWidth - 10;
                //bindingWidth.Mode = BindingMode.OneWay;
                //ganttControl.SetBinding(GanttControl.WidthProperty, bindingWidth);
                ganttControl.Width = dataContextGanttView.HeaderWidth - 10;

                PART_GanttHeader.Content = ganttControl;
            }
        }

        private void PART_ColumnCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetPeriod();

            if (ganttControl != null)
            {
                ganttControl.ColumnCount = (int)PART_ColumnCount.Value;
            }
        }

        private void PART_GanttHeader_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ganttControl != null)
            {
                ganttControl.Width = dataContextGanttView.HeaderWidth - 10 >= 0 ? dataContextGanttView.HeaderWidth - 10 : 0;
            }
        }
    }


    public class DataContextGanttView : ObservableObject
    {
        private List<GanttData> _GanttDatas;
        public List<GanttData> GanttDatas { get => _GanttDatas; set => SetValue(ref _GanttDatas, value); }

        private int _ColumnCount = 30;
        public int ColumnCount { get => _ColumnCount; set => SetValue(ref _ColumnCount, value); }

        private double _HeaderWidth = 790;
        public double HeaderWidth { get => _HeaderWidth; set => SetValue(ref _HeaderWidth, value); }

        private DateTime _LastDate = DateTime.Now;
        public DateTime LastDate { get => _LastDate; set => SetValue(ref _LastDate, value); }
    }


    public class GanttData
    {
        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public DateTime LastActivity { get; set; }

        public List<GanttValue> DateTimes { get; set; }


        public RelayCommand<Guid> GoToGame => PluginDatabase.GoToGame;

        public bool GameExist => PluginDatabase.PlayniteApi.Database.Games.Get(Id) != null;
    }

    public class GanttValue
    {
        public DateTime PlayDate { get; set; }
        public ulong PlayTime { get; set; }
    }
}
