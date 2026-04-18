using GameActivity.Controls;
using GameActivity.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GameActivity.Views
{
    /// <summary>
    /// Gantt activity view: data and period logic live in <see cref="GameActivityGanttViewModel"/>;
    /// code-behind only hosts the header <see cref="GanttControl"/> (axis row) and syncs its width.
    /// </summary>
    public partial class GameActivityGanttView : UserControl
    {
        private GanttControl _headerGanttControl;
        private GameActivityGanttViewModel _viewModel;

        public GameActivityGanttView()
        {
            InitializeComponent();
            _viewModel = new GameActivityGanttViewModel();
            DataContext = _viewModel;
            Loaded += GameActivityGanttView_Loaded;
            Unloaded += GameActivityGanttView_Unloaded;
        }

        private void GameActivityGanttView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            }
        }

        private void GameActivityGanttView_Loaded(object sender, RoutedEventArgs e)
        {
            if (PART_GanttHeader == null || _viewModel == null)
            {
                return;
            }

            if (PART_GanttHeader.Content != null)
            {
                Loaded -= GameActivityGanttView_Loaded;
                return;
            }

            _headerGanttControl = new GanttControl
            {
                OnlyDate = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                Height = 80
            };

            _headerGanttControl.SetBinding(GanttControl.ColumnCountProperty, new Binding(nameof(GameActivityGanttViewModel.ColumnCount)) { Source = _viewModel });
            _headerGanttControl.SetBinding(GanttControl.LastDateProperty, new Binding(nameof(GameActivityGanttViewModel.LastDate)) { Source = _viewModel });

            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
            ApplyHeaderGanttWidth();

            PART_GanttHeader.Content = _headerGanttControl;
            Loaded -= GameActivityGanttView_Loaded;
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GameActivityGanttViewModel.HeaderWidth))
            {
                ApplyHeaderGanttWidth();
            }
        }

        private void ApplyHeaderGanttWidth()
        {
            if (_headerGanttControl == null || _viewModel == null)
            {
                return;
            }

            double w = _viewModel.HeaderWidth - 10;
            _headerGanttControl.Width = w >= 0 ? w : 0;
        }
    }
}
