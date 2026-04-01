using GameActivity.ViewModels;
using Playnite.SDK.Models;
using System.Windows;
using System.Windows.Controls;

namespace GameActivity.Views
{
    public partial class GameActivityMergeTime : UserControl
    {
        private readonly GameActivityMergeTimeViewModel _vm;

        public GameActivityMergeTime(Game game)
        {
            _vm = new GameActivityMergeTimeViewModel(game);
            _vm.CloseRequested += OnCloseRequested;

            DataContext = _vm;
            InitializeComponent();
        }

        private void OnCloseRequested(object sender, System.EventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}
