using CommonPluginsShared;
using GameActivity.Models;
using GameActivity.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using CommonPluginsShared.UI;
using System.Collections.Generic;

namespace GameActivity.ViewModels
{
    /// <summary>
    /// ViewModel for the <c>GamesDataMismatch</c> view.
    /// Binds directly to <see cref="GameActivities"/> – no item-level wrapper needed.
    /// Per-row commands receive the target <see cref="Guid"/> as CommandParameter.
    /// </summary>
    public class GamesDataMismatchViewModel : ObservableObject
    {
        private static ILogger Logger => LogManager.GetLogger();
        private static GameActivityDatabase PluginDatabase => GameActivity.PluginDatabase;

        // ── Observable list ──────────────────────────────────────────────────────

        private ObservableCollection<GameActivities> _items;

        /// <summary>Games whose GA data differs from Playnite.</summary>
        public ObservableCollection<GameActivities> Items
        {
            get => _items;
            private set
            {
                SetValue(ref _items, value);
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(EmptyStateVisibility));
                ApplyAllCommand.CanExecute();
            }
        }

        /// <summary>True when at least one mismatch is present.</summary>
        public bool HasData => _items?.Count > 0;

        /// <summary>Drives the empty-state overlay visibility.</summary>
        public Visibility EmptyStateVisibility => HasData ? Visibility.Collapsed : Visibility.Visible;

        // ── Commands ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Overwrites Playnite data with GA data for a single game.
        /// CommandParameter: <see cref="Guid"/> (the game Id).
        /// </summary>
        public RelayCommand<Guid> ApplySingleCommand { get; }

        /// <summary>
        /// Navigates to a game in the Playnite library view.
        /// Reuses the shared static command; CommandParameter: <see cref="Guid"/>.
        /// </summary>
        public RelayCommand<Guid> GoToGameCommand => CommandsNavigation.GoToGame;

        /// <summary>
        /// Overwrites Playnite data with GA data for ALL listed games,
        /// with confirmation dialog and cancellable progress bar.
        /// Disabled automatically when the list is empty.
        /// </summary>
        public RelayCommand ApplyAllCommand { get; }

        // ── Constructor ──────────────────────────────────────────────────────────

        public GamesDataMismatchViewModel()
        {
            ApplySingleCommand = new RelayCommand<Guid>(ExecuteApplySingle);
            ApplyAllCommand = new RelayCommand(ExecuteApplyAll, () => HasData);
            RefreshData();
        }

        // ── Data ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reloads the mismatch list from the plugin database.
        /// Called on construction and after every successful apply.
        /// </summary>
        private void RefreshData()
        {
            Items = PluginDatabase.GetGamesDataMismatch(false).ToObservable();
        }

        // ── Command implementations ──────────────────────────────────────────────

        /// <summary>
        /// Applies GA data to Playnite for a single game then refreshes the list.
        /// </summary>
        /// <param name="id">Playnite game Id passed as CommandParameter from the row.</param>
        private void ExecuteApplySingle(Guid id)
        {
            try
            {
                if (!ApplyToDatabase(id))
                {
                    return;
                }

                Logger.Info($"ApplySingle – applied GA data for game {id}.");
                RefreshData();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        /// <summary>
        /// Bulk-applies GA data to Playnite for all listed games.
        /// Shows a confirmation dialog and a cancellable progress indicator.
        /// </summary>
        private void ExecuteApplyAll()
        {
            if (!HasData)
            {
                return;
            }

            MessageBoxResult confirm = API.Instance.Dialogs.ShowMessage(
                ResourceProvider.GetString("LOCGaPlayniteToGaWarning"),
                ResourceProvider.GetString("LOCGaDataMismatchApplyAllConfirmTitle"),
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.OK)
            {
                return;
            }

            // Snapshot ids so the lambda does not capture the live collection.
            Guid[] ids = _items.Select(x => x.Id).ToArray();

            GlobalProgressOptions progressOptions = new GlobalProgressOptions(
                $"{PluginDatabase.PluginName} – {ResourceProvider.GetString("LOCCommonProcessing")}")
            {
                Cancelable = true,
                IsIndeterminate = false
            };

            API.Instance.Dialogs.ActivateGlobalProgress((progress) =>
            {
                progress.ProgressMaxValue = ids.Length;
                Stopwatch sw = Stopwatch.StartNew();

                API.Instance.Database.BeginBufferUpdate();
                try
                {
                    foreach (Guid id in ids)
                    {
                        if (progress.CancelToken.IsCancellationRequested)
                        {
                            break;
                        }

                        ApplyToDatabase(id);
                        progress.CurrentProgressValue++;
                    }
                }
                finally
                {
                    API.Instance.Database.EndBufferUpdate();
                }

                sw.Stop();
                Logger.Info(
                    $"ApplyAll{(progress.CancelToken.IsCancellationRequested ? " (canceled)" : string.Empty)} – " +
                    $"{sw.Elapsed.Minutes:00}:{sw.Elapsed.Seconds:00}.{sw.Elapsed.Milliseconds / 10:00} " +
                    $"for {progress.CurrentProgressValue}/{ids.Length} items.");

            }, progressOptions);

            RefreshData();
        }

        // ── Shared helper ────────────────────────────────────────────────────────

        /// <summary>
        /// Writes GA session data into the Playnite game record.
        /// </summary>
        /// <param name="id">Game Id to update.</param>
        /// <returns>True if the update succeeded.</returns>
        private static bool ApplyToDatabase(Guid id)
        {
            GameActivities ga = PluginDatabase.Get(id);
            Game game = API.Instance.Database.Games.Get(id);

            if (ga == null || game == null)
            {
                Logger.Warn($"ApplyToDatabase – skipping {id}: GA={ga != null}, Game={game != null}.");
                return false;
            }

            game.PlayCount = ga.Count;
            game.Playtime = ga.SessionPlaytime;
            game.LastActivity = ga.GetLastSessionActivity()?.DateSession;
            API.Instance.Database.Games.Update(game);
            return true;
        }
    }
}