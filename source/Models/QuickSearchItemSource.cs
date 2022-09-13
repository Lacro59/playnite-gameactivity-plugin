using CommonPluginsShared;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using GameActivity.Services;
using Playnite.SDK;
using QuickSearch.SearchItems;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameActivity.Models
{
    class QuickSearchItemSource : ISearchSubItemSource<string>
    {
        private ActivityDatabase PluginDatabase = GameActivity.PluginDatabase;


        public string Prefix => PluginDatabase.PluginName;

        public bool DisplayAllIfQueryIsEmpty => true;

        public string Icon => Path.Combine(PluginDatabase.Paths.PluginPath, "Resources", "command-line.png");


        public IEnumerable<ISearchItem<string>> GetItems()
        {
            return null;
        }

        public IEnumerable<ISearchItem<string>> GetItems(string query)
        {
            if (query.IsEqual("fps"))
            {
                return new List<ISearchItem<string>>
                {
                    new CommandItem("<", new List<CommandAction>(), "example: fps < 30", Icon),
                    new CommandItem("<>", new List<CommandAction>(), "example: fps 30 <> 60", Icon),
                    new CommandItem(">", new List<CommandAction>(), "example: fps > 60", Icon),
                }.AsEnumerable();
            }
        
            if (query.IsEqual("time"))
            {
                return new List<ISearchItem<string>>
                {
                    new CommandItem("<", new List<CommandAction>(), "example: time < 30 s", Icon),
                    new CommandItem("<>", new List<CommandAction>(), "example: time 30 min <> 1 h", Icon),
                    new CommandItem(">", new List<CommandAction>(), "example: time > 2 h", Icon),
                }.AsEnumerable();
            }
        
            if (query.IsEqual("date"))
            {
                return new List<ISearchItem<string>>
                {
                    new CommandItem("<", new List<CommandAction>(), "example: date < 2021-11-19", Icon),
                    new CommandItem("<>", new List<CommandAction>(), "example: date 2021-11-19 <> 2021-11-25", Icon),
                    new CommandItem(">", new List<CommandAction>(), "example: date > 2021-11-19", Icon),
                }.AsEnumerable();
            }

            return new List<ISearchItem<string>>
            {
                new CommandItem("fps", new List<CommandAction>(), ResourceProvider.GetString("LOCGaQuickSearchByFPS"), Icon),
                new CommandItem("time", new List<CommandAction>(), ResourceProvider.GetString("LOCGaQuickSearchByTime"), Icon),
                new CommandItem("date", new List<CommandAction>(), ResourceProvider.GetString("LOCGaQuickSearchByDate"), Icon),
            }.AsEnumerable();
        }

        public Task<IEnumerable<ISearchItem<string>>> GetItemsTask(string query, IReadOnlyList<Candidate> addedItems)
        {
            var parameters = GetParameters(query);
            if (parameters.Count > 0)
            {
                switch (parameters[0].ToLower())
                {
                    case "fps":
                        return SearchByFps(query);

                    case "time":
                        return SearchByTime(query);

                    case "date":
                        return SearchByDate(query);
                }
            }

            return null;
        }


        private List<string> GetParameters(string query)
        {
            List<string> parameters = query.Split(' ').ToList();
            if (parameters.Count > 1 && parameters[0].IsNullOrEmpty())
            {
                parameters.RemoveAt(0);
            }
            return parameters;
        }

        private CommandItem GetCommandItem(GameActivities data, string query)
        {
            DefaultIconConverter defaultIconConverter = new DefaultIconConverter();

            LocalDateTimeConverter localDateTimeConverter = new LocalDateTimeConverter();

            var title = data.Name;
            var AvgFpsAllSession = data.ItemsDetails.AvgFpsAllSession.ToString();
            var icon = defaultIconConverter.Convert(data.Icon, null, null, null).ToString();
            var dateSession = localDateTimeConverter.Convert(data.LastActivity, null, null, CultureInfo.CurrentCulture).ToString();
            var LastSession = data.LastActivity == null ? string.Empty : ResourceProvider.GetString("LOCLastPlayedLabel") 
                    + " " + dateSession;

            var item = new CommandItem(title, () => PluginDatabase.PlayniteApi.MainView.SelectGame(data.Id), "", null, icon)
            {
                IconChar = null,
                BottomLeft = PlayniteTools.GetSourceName(data.Id),
                BottomCenter = null,
                BottomRight = ResourceProvider.GetString("LOCGameActivityAvgFps") + " " + AvgFpsAllSession,
                TopLeft = title,
                TopRight = LastSession,
                Keys = new List<ISearchKey<string>>() { new CommandItemKey() { Key = query, Weight = 1 } }
            };

            return item;
        }

        private double GetElapsedSeconde(string value, string type)
        {
            switch (type.ToLower())
            {
                case "h":
                    double h = double.Parse(value);
                    return h * 3600;

                case "min":
                    double m = double.Parse(value);
                    return m * 60;


                case "s":
                    return double.Parse(value);
            }

            return 0;
        } 

        private List<KeyValuePair<Guid, GameActivities>> GetDb(ConcurrentDictionary<Guid, GameActivities> db)
        {
            return db.Where(x => PluginDatabase.PlayniteApi.Database.Games.Get(x.Key) != null).ToList();
        }


        private Task<IEnumerable<ISearchItem<string>>> SearchByFps(string query)
        {
            var parameters = GetParameters(query);
            var db = GetDb(PluginDatabase.Database.Items).Where(x => x.Value.ItemsDetails.AvgFpsAllSession != 0).ToList();

            if (parameters.Count == 3)
            {
                return Task.Run(() =>
                {
                    var search = new List<ISearchItem<string>>();
                    switch (parameters[1])
                    {
                        case ">":
                            try
                            {
                                double fps = double.Parse(parameters[2]);
                                foreach (var data in db)
                                {
                                    if (data.Value.ItemsDetails.AvgFpsAllSession >= fps)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;


                        case "<":
                            try
                            {
                                double fps = double.Parse(parameters[2]);
                                foreach (var data in db)
                                {
                                    if (data.Value.ItemsDetails.AvgFpsAllSession <= fps)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            if (parameters.Count == 4)
            {
                return Task.Run(() =>
                {
                    var search = new List<ISearchItem<string>>();
                    switch (parameters[2])
                    {
                        case "<>":
                            try
                            {
                                double fpsMin = double.Parse(parameters[1]);
                                double fpsMax = double.Parse(parameters[3]);
                                foreach (var data in db)
                                {
                                    if (data.Value.ItemsDetails.AvgFpsAllSession >= fpsMin && data.Value.ItemsDetails.AvgFpsAllSession <= fpsMax)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            return null;
        }

        private Task<IEnumerable<ISearchItem<string>>> SearchByTime(string query)
        {
            var parameters = GetParameters(query);
            var db = GetDb(PluginDatabase.Database.Items);

            if (parameters.Count == 4)
            {
                return Task.Run(() =>
                {
                    var search = new List<ISearchItem<string>>();

                    switch (parameters[1])
                    {
                        case ">":
                            try
                            {
                                double s = GetElapsedSeconde(parameters[2], parameters[3]);
                                foreach (var data in db)
                                {
                                    if (data.Value.Items.Where(x => x.ElapsedSeconds >= s).Count() > 0)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;


                        case "<":
                            try
                            {
                                double s = GetElapsedSeconde(parameters[2], parameters[3]);
                                foreach (var data in db)
                                {
                                    if (data.Value.Items.Where(x => x.ElapsedSeconds <= s).Count() > 0)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            if (parameters.Count == 6)
            {
                return Task.Run(() =>
                {
                    var search = new List<ISearchItem<string>>();
                    switch (parameters[3])
                    {
                        case "<>":
                            try
                            {
                                double sMin = GetElapsedSeconde(parameters[1], parameters[2]);
                                double sMax = GetElapsedSeconde(parameters[4], parameters[5]);
                                foreach (var data in db)
                                {
                                    if (data.Value.Items.Where(x => x.ElapsedSeconds >= sMin && x.ElapsedSeconds <= sMax).Count() > 0)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            return null;
        }

        private Task<IEnumerable<ISearchItem<string>>> SearchByDate(string query)
        {
            var parameters = GetParameters(query);
            var db = GetDb(PluginDatabase.Database.Items);

            if (parameters.Count == 3)
            {
                return Task.Run(() =>
                {
                    var search = new List<ISearchItem<string>>();
                    switch (parameters[1])
                    {
                        case ">":
                            try
                            {
                                DateTime date = DateTime.Parse(parameters[2] + " 00:00:00");
                                foreach (var data in db)
                                {
                                    if (data.Value.Items.Where(x => x.DateSession >= date).Count() > 0)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;


                        case "<":
                            try
                            {
                                DateTime date = DateTime.Parse(parameters[2] + " 23:59:59");
                                foreach (var data in db)
                                {
                                    if (data.Value.Items.Where(x => x.DateSession <= date).Count() > 0)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            if (parameters.Count == 4)
            {
                return Task.Run(() =>
                {
                    var search = new List<ISearchItem<string>>();
                    switch (parameters[2])
                    {
                        case "<>":
                            try
                            {
                                DateTime dateMin = DateTime.Parse(parameters[1] + " 00:00:00");
                                DateTime dateMax = DateTime.Parse(parameters[3] + " 23:59:59");
                                foreach (var data in db)
                                {
                                    if (data.Value.Items.Where(x => x.DateSession >= dateMin && x.DateSession <= dateMax).Count() > 0)
                                    {
                                        search.Add(GetCommandItem(data.Value, query));
                                    }
                                }
                            }
                            catch { }
                            break;
                    }

                    return search.AsEnumerable();
                });
            }

            return null;
        }
    }
}
