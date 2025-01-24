using GameActivity.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPluginsShared.Collections;
using System;
using System.Collections.Generic;
using CommonPlayniteShared.Common;
using CommonPlayniteShared.Converters;
using System.Globalization;
using CommonPluginsShared;
using System.IO;
using System.Linq;
using System.Diagnostics;
using GameActivity.Models.ExportData;
using CommonPluginsShared.Extensions;

namespace GameActivity.Services
{
    public class ActivityDatabase : PluginDatabaseObject<GameActivitySettingsViewModel, GameActivitiesCollection, GameActivities, Activity>
    {
        private LocalSystem _localSystem;
        public LocalSystem LocalSystem
        {
            get
            {
                if (_localSystem == null)
                {
                    _localSystem = new LocalSystem(Path.Combine(Paths.PluginUserDataPath, $"Configurations.json"), false);
                }
                return _localSystem;
            }
        }


        public ActivityDatabase(GameActivitySettingsViewModel pluginSettings, string pluginUserDataPath) : base(pluginSettings, "GameActivity", pluginUserDataPath)
        {
        }


        protected override bool LoadDatabase()
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                Database = new GameActivitiesCollection(Paths.PluginDatabasePath);
                Database.SetGameInfoDetails<Activity, ActivityDetails>();

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"LoadDatabase with {Database.Count} items - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return false;
            }

            return true;
        }


        public override GameActivities Get(Guid id, bool onlyCache = false, bool force = false)
        {
            GameActivities gameActivities = GetOnlyCache(id);
            if (gameActivities == null)
            {
                Game game = API.Instance.Database.Games.Get(id);
                if (game != null)
                {
                    gameActivities = GetDefault(game);
                    Add(gameActivities);
                }
            }
            return gameActivities;
        }


        public override void SetThemesResources(Game game)
        {
            GameActivities gameActivities = Get(game, true);

            if (gameActivities == null)
            {
                PluginSettings.Settings.HasData = false;
                PluginSettings.Settings.HasDataLog = false;

                PluginSettings.Settings.LastDateSession = string.Empty;
                PluginSettings.Settings.LastDateTimeSession = string.Empty;

                PluginSettings.Settings.LastPlaytimeSession = string.Empty;
                PluginSettings.Settings.AvgFpsAllSession = 0;

                return;
            }

            PluginSettings.Settings.HasData = gameActivities.HasData;
            PluginSettings.Settings.HasDataLog = gameActivities.HasDataDetails();

            PluginSettings.Settings.LastDateSession = gameActivities.GetLastSession().ToLocalTime().ToString(Constants.DateUiFormat);
            PluginSettings.Settings.LastDateTimeSession = gameActivities.GetLastSession().ToLocalTime().ToString(Constants.DateUiFormat)
                + " " + gameActivities.GetLastSession().ToLocalTime().ToString(Constants.TimeUiFormat);

            PlayTimeToStringConverter converter = new PlayTimeToStringConverter();
            string playtime = (string)converter.Convert(gameActivities.GetLastSessionActivity().ElapsedSeconds, null, null, CultureInfo.CurrentCulture);
            PluginSettings.Settings.LastPlaytimeSession = playtime;

            PluginSettings.Settings.AvgFpsAllSession = gameActivities.ItemsDetails.AvgFpsAllSession;

            PluginSettings.Settings.RecentActivity = gameActivities.GetRecentActivity();
        }

        public override void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            if (e?.UpdatedItems != null)
            {
                foreach (ItemUpdateEvent<Game> GameUpdated in e.UpdatedItems)
                {
                    Database.SetGameInfoDetails<Activity, ActivityDetails>(GameUpdated.NewData.Id);
                    _ = Get(GameUpdated.NewData.Id);
                }
            }
        }


        /// <summary>
        /// get list GameActivity in ActivityDatabase.
        /// </summary>
        /// <returns></returns>
        public List<GameActivities> GetListGameActivity()
        {
            return Database.ToList();
        }


        public override PluginDataBaseGameBase MergeData(Guid fromId, Guid toId)
        {
            try
            {
                GameActivities fromData = Get(fromId, true);
                GameActivities toData = Get(toId, true);

                toData.Items.AddRange(fromData.Items);
                fromData.ItemsDetails.Items.ForEach(x =>
                {
                    _ = toData.ItemsDetails.Items.TryAdd(x.Key, x.Value);
                });

                return toData;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return null;
            }
        }


        internal override string GetCsvData(GlobalProgressActionArgs a, bool minimum)
        {
            List<string> header = minimum
                ? new List<string>
                {
                    ResourceProvider.GetString("LOCGameNameTitle"),
                    ResourceProvider.GetString("LOCSourceLabel"),
                    ResourceProvider.GetString("LOCPlayCountLabel"),
                    ResourceProvider.GetString("LOCStatsTotalPlayTime"),
                    ResourceProvider.GetString("LOCStatsTotalPlayTime"),
                    ResourceProvider.GetString("LOCGameActivityLvGamesLastActivity"),
                    ResourceProvider.GetString("LOCGameActivityAvgCpu"),
                    ResourceProvider.GetString("LOCGameActivityAvgRam"),
                    ResourceProvider.GetString("LOCGameActivityAvgGpu"),
                    ResourceProvider.GetString("LOCGameActivityAvgFps"),
                    ResourceProvider.GetString("LOCGameActivityAvgCpuT"),
                    ResourceProvider.GetString("LOCGameActivityAvgGpuT"),
                    ResourceProvider.GetString("LOCGameActivityAvgCpuP"),
                    ResourceProvider.GetString("LOCGameActivityAvgGpuP")
                }
                : new List<string>
                {
                    ResourceProvider.GetString("LOCGameNameTitle"),
                    ResourceProvider.GetString("LOCSourceLabel"),
                    ResourceProvider.GetString("LOCGameActivityDateSession"),
                    ResourceProvider.GetString("LOCGameActivityDateLog"),
                    ResourceProvider.GetString("LOCTimePlayed"),
                    ResourceProvider.GetString("LOCTimePlayed"),
                    ResourceProvider.GetString("LOCGameActivityPCName"),
                    ResourceProvider.GetString("LOCGameActivityCpuUsage"),
                    ResourceProvider.GetString("LOCGameActivityRamUsage"),
                    ResourceProvider.GetString("LOCGameActivityGpuUsage"),
                    ResourceProvider.GetString("LOCGameActivityFps"),
                    ResourceProvider.GetString("LOCGameActivityCpuTemp"),
                    ResourceProvider.GetString("LOCGameActivityGpuTemp"),
                    ResourceProvider.GetString("LOCGameActivityCpuPower"),
                    ResourceProvider.GetString("LOCGameActivityGpuPower")
                }; ;

            a.ProgressMaxValue = minimum
                ? Database.Items?.Where(x => x.Value.HasData)?.Count() ?? 0
                : Database.Items?.Where(x => x.Value.HasData)?.Sum(x => x.Value.ItemsDetails.Count) ?? 0;

            List<ExportData> exportDatas = new List<ExportData>();
            List<ExportDataAll> exportDataAlls = new List<ExportDataAll>();

            Database.Items?.Where(x => x.Value.HasData && x.Value?.Game != null)?.ForEach(x =>
            {
                if (a.CancelToken.IsCancellationRequested)
                {
                    return;
                }

                if (minimum)
                {
                    a.Text = $"{PluginName} - {ResourceProvider.GetString("LOCCommonExtracting")}"
                       + "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}"
                       + "\n" + x.Value.Game?.Name + (x.Value.Game?.Source == null ? string.Empty : $" ({x.Value.Game?.Source.Name})");

                    TimeSpan ts = new TimeSpan(0, 0, (int)x.Value.Game.Playtime);
                    string playtimeFormat = string.Format("{0:00}:{1:00}:{1:00}", ts.Hours, ts.Minutes, ts.Seconds);

                    ExportData exportData = new ExportData
                    {
                        Name = x.Value.Name,
                        SourceName = x.Value.Source?.Name ?? x.Value.Platforms?.First()?.Name ?? "Playnite",
                        PlayCount = x.Value.Count,
                        Playtime = x.Value.Playtime,
                        PlaytimeFormat = playtimeFormat,
                        LastSession = x.Value.GetLastSession(),
                        AvgCPU = x.Value.AvgCPU(x.Value.GetLastSession()),
                        AvgRAM = x.Value.AvgRAM(x.Value.GetLastSession()),
                        AvgGPU = x.Value.AvgGPU(x.Value.GetLastSession()),
                        AvgFPS = x.Value.AvgFPS(x.Value.GetLastSession()),
                        AvgCPUT = x.Value.AvgCPUT(x.Value.GetLastSession()),
                        AvgGPUT = x.Value.AvgGPUT(x.Value.GetLastSession()),
                        AvgCPUP = x.Value.AvgCPUP(x.Value.GetLastSession()),
                        AvgGPUP = x.Value.AvgGPUP(x.Value.GetLastSession())
                    };
                    exportDatas.Add(exportData);
                    a.CurrentProgressValue++;
                }
                else
                {
                    x.Value.Items.ForEach(y =>
                    {
                        if (a.CancelToken.IsCancellationRequested)
                        {
                            return;
                        }

                        a.Text = $"{PluginName} - {ResourceProvider.GetString("LOCCommonExtracting")}"
                            + "\n\n" + $"{a.CurrentProgressValue}/{a.ProgressMaxValue}"
                            + "\n" + x.Value.Game?.Name + (x.Value.Game?.Source == null ? string.Empty : $" ({x.Value.Game?.Source.Name})");

                        TimeSpan ts = new TimeSpan(0, 0, (int)y.ElapsedSeconds);
                        string playtimeFormat = string.Format("{0:00}:{1:00}:{1:00}", ts.Hours, ts.Minutes, ts.Seconds);

                        List<ActivityDetailsData> details = x.Value.ItemsDetails.Get((DateTime)y.DateSession);
                        details.ForEach(z =>
                        {
                            ExportDataAll exportDataAll = new ExportDataAll
                            {
                                Name = x.Value.Name,
                                SourceName = x.Value.Source?.Name ?? x.Value.Platforms?.First()?.Name ?? "Playnite",
                                Session = y.DateSession,
                                DateTimeValue = z.Datelog,
                                Playtime = y.ElapsedSeconds,
                                PlaytimeFormat = playtimeFormat,
                                PC = y.Configuration.Name,
                                CPU = z.CPU,
                                RAM = z.RAM,
                                GPU = z.GPU,
                                FPS = z.FPS,
                                CPUT = z.CPUT,
                                GPUT = z.GPUT,
                                CPUP = z.CPUP,
                                GPUP = z.GPUP
                            };
                            exportDataAlls.Add(exportDataAll);
                        });
                        a.CurrentProgressValue++;
                    });
                }
            });

            return minimum ? exportDatas.ToCsv(false, ";", false, header) : exportDataAlls.ToCsv(false, ";", false, header);
        }


        public IEnumerable<GameActivities> GetGamesDataMismatch(bool withHidden)
        {
            try
            {
                return Database.Items
                    ?.Where(x => x.Value.GameExist && (x.Value.SessionPlaytime != x.Value.Game.Playtime || x.Value.Game.PlayCount != (ulong)x.Value.Count) && (!x.Value.Game.Hidden || withHidden))
                    ?.Select(x => x.Value) ?? Enumerable.Empty<GameActivities>();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
            }
            return Enumerable.Empty<GameActivities>();
        }
    }
}
