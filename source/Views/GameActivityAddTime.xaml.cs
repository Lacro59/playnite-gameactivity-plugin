using GameActivity.Models;
using GameActivity.Services;
using GameActivity.ViewModels;
using Playnite.SDK.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace GameActivity.Views
{
    /// <summary>
    /// Code-behind for GameActivityAddTime.
    /// Responsibilities limited to:
    ///   1. Creating the ViewModel and wiring CloseRequested.
    ///   2. Exposing the result activity to callers.
    /// Zero business logic lives here.
    /// </summary>
    public partial class GameActivityAddTime : UserControl
    {
        /// <summary>Read by the caller after the dialog closes.</summary>
        public Activity Activity => (_vm != null) ? _vm.ResultActivity : null;

        private readonly GameActivityAddTimeViewModel _vm;

        public GameActivityAddTime(GameActivity plugin, Game game, Activity activityEdit)
        {
            _vm = new GameActivityAddTimeViewModel(plugin, game, activityEdit);
            _vm.CloseRequested += OnCloseRequested;

            DataContext = _vm;
            InitializeComponent();
        }

        /// <summary>Closes the host Window when the ViewModel signals it.</summary>
        private void OnCloseRequested(object sender, EventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}