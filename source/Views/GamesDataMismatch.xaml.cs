using GameActivity.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GameActivity.Views
{
    /// <summary>
    /// Code-behind for GamesDataMismatch.xaml.
    /// All business logic lives in <see cref="GamesDataMismatchViewModel"/>.
    /// This file only wires the DataContext and delegates window closure to the VM command.
    /// </summary>
    public partial class GamesDataMismatch : UserControl
    {
        public GamesDataMismatch()
        {
            InitializeComponent();
            DataContext = new GamesDataMismatchViewModel();
        }
    }
}