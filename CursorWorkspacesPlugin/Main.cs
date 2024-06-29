using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using System.Windows.Controls;
using Wox.Plugin;
using Community.PowerToys.Run.Plugin.CursorWorkspacesPlugin.RemoteMachinesHelper;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows;
using Community.PowerToys.Run.Plugin.VSCodeWorkspaces.WorkspacesHelper;

namespace Community.PowerToys.Run.Plugin.CursorWorkspacesPlugin
{
    public class Main : IPlugin, IPluginI18n, IContextMenu, ISettingProvider, IReloadable, IDisposable, IDelayedExecutionPlugin
    {
        private const string Setting = nameof(Setting);

        // current value of the setting
        private bool _setting;

        private PluginInitContext _context;

        private string _iconPath;

        private bool _disposed;

        private VSCodeRemoteMachinesApi _remoteMachinesApi = new VSCodeRemoteMachinesApi();

        private VSCodeWorkspacesApi _workspacesApi = new VSCodeWorkspacesApi();

        public string Name => Properties.Resources.plugin_name;

        public string Description => Properties.Resources.plugin_description;

        // TODO: remove dash from ID below and inside plugin.json
        public static string PluginID => "59576c9d-5a12-45b0-bf27-ceecb9f6d74e";

        // TODO: add additional options (optional)
        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                Key = Setting,
                DisplayLabel = Properties.Resources.plugin_setting,
                Value = false,
            },
        };

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            _setting = settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == Setting)?.Value ?? false;
        }

        // TODO: return context menus for each Result (optional)
        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return new List<ContextMenuResult>(0);
        }

        // TODO: return query results
        public List<Result> Query(Query query)
        {
            ArgumentNullException.ThrowIfNull(query);

            var results = new List<Result>();
            

            if (string.IsNullOrEmpty(query.Search))
            {
                return results;
            }


            _workspacesApi.Workspaces.ForEach(a =>
            {
                var title = a.WorkspaceType == WorkspaceType.ProjectFolder ? a.FolderName : a.FolderName.Replace(".code-workspace", $" ()");

                var typeWorkspace = a.WorkspaceEnvironmentToString();
                if (a.WorkspaceEnvironment != WorkspaceEnvironment.Local)
                {
                    title = $"{title}{(a.ExtraInfo != null ? $" - {a.ExtraInfo}" : string.Empty)} ({typeWorkspace})";
                }

                results.Add(new Result
                {
                    Title = title,
                    Action = c =>
                    {
                        bool hide;
                        try
                        {
                            var process = new ProcessStartInfo
                            {
                                FileName = "cursor",
                                UseShellExecute = true,
                                Arguments = a.WorkspaceType == WorkspaceType.ProjectFolder ? $"--folder-uri {a.Path}" : $"--file-uri {a.Path}",
                                WindowStyle = ProcessWindowStyle.Hidden,
                            };
                            Process.Start(process);

                            hide = true;
                        }
                        catch (Win32Exception)
                        {
                            var name = $"Plugin: {_context.CurrentPluginMetadata.Name}";
                            var msg = "Can't Open this file";
                            _context.API.ShowMsg(name, msg, string.Empty);
                            hide = false;
                        }

                        return hide;
                    },
                    ContextData = a,
                });
            });

            

            foreach (var a in _remoteMachinesApi.Machines)
            {
                var title = $"{a.Host}";

                if (a.User != null && a.User != string.Empty && a.HostName != null && a.HostName != string.Empty)
                {
                    title += $" [{a.User}@{a.HostName}]";
                }


                results.Add(new Result
                {
                    Title = title,
                    Action = c =>
                    {
                        bool hide;
                        try
                        {
                            var process = new ProcessStartInfo()
                            {
                                FileName = "cursor",
                                UseShellExecute = true,
                                Arguments = $"--new-window --enable-proposed-api ms-vscode-remote.remote-ssh --remote ssh-remote+{((char)34) + a.Host + ((char)34)}",
                                WindowStyle = ProcessWindowStyle.Hidden,
                            };
                            Process.Start(process);

                            hide = true;
                        }
                        catch (Win32Exception)
                        {
                            var name = $"Plugin: {_context.CurrentPluginMetadata.Name}";
                            var msg = "Can't Open this file";
                            _context.API.ShowMsg(name, msg, string.Empty);
                            hide = false;
                        }

                        return hide;
                    },
                    ContextData = a,
                });
            }

            results = results.Where(a => a.Title.Contains(query.Search, StringComparison.InvariantCultureIgnoreCase)).ToList();

            results.ForEach(x =>
            {
                if (x.Score == 0)
                {
                    x.Score = 100;
                }

                // intersect the title with the query
                var intersection = Convert.ToInt32(x.Title.ToLowerInvariant().Intersect(query.Search.ToLowerInvariant()).Count() * query.Search.Length);
                var differenceWithQuery = Convert.ToInt32((x.Title.Length - intersection) * query.Search.Length * 0.7);
                x.Score = x.Score - differenceWithQuery + intersection;

                // if is a remote machine give it 12 extra points
                if (x.ContextData is VSCodeRemoteMachine)
                {
                    x.Score = Convert.ToInt32(x.Score + (intersection * 2));
                }
            });

            results = results.OrderByDescending(x => x.Score).ToList();
            if (query.Search == string.Empty || query.Search.Replace(" ", string.Empty) == string.Empty)
            {
                results = results.OrderBy(x => x.Title).ToList();
            }
            
            return results;
        }

        public List<Result> Query(Query query, bool delayedExecution)
        {
            return new List<Result>();
        }

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        public string GetTranslatedPluginTitle()
        {
            return Properties.Resources.plugin_name;
        }

        public string GetTranslatedPluginDescription()
        {
            return Properties.Resources.plugin_description;
        }

        private void OnThemeChanged(Theme oldtheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            if (theme == Theme.Light || theme == Theme.HighContrastWhite)
            {
                _iconPath = "Images/CursorWorkspacesPlugin.light.png";
            }
            else
            {
                _iconPath = "Images/CursorWorkspacesPlugin.dark.png";
            }
        }

        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void ReloadData()
        {
            if (_context is null)
            {
                return;
            }

            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_context != null && _context.API != null)
                {
                    _context.API.ThemeChanged -= OnThemeChanged;
                }

                _disposed = true;
            }
        }
    }
}
