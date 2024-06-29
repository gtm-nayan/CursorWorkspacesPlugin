// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text.Json;
using Community.PowerToys.Run.Plugin.CursorWorkspacesPlugin.SshConfigParser;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.CursorWorkspacesPlugin.RemoteMachinesHelper
{
    public class VSCodeRemoteMachinesApi
    {
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        public VSCodeRemoteMachinesApi()
        {
        }

        public List<VSCodeRemoteMachine> Machines
        {
            get
            {
                var results = new List<VSCodeRemoteMachine>();

                {
                    // settings.json contains path of ssh_config
                    var vscode_settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cursor", "User\\settings.json");

                    if (File.Exists(vscode_settings))
                    {
                        var fileContent = File.ReadAllText(vscode_settings);

                        try
                        {
                            JsonElement vscodeSettingsFile = JsonSerializer.Deserialize<JsonElement>(fileContent, _serializerOptions);
                            if (vscodeSettingsFile.TryGetProperty("remote.SSH.configFile", out var pathElement))
                            {
                                var path = pathElement.GetString();

                                if (File.Exists(path))
                                {
                                    foreach (SshHost h in SshConfig.ParseFile(path))
                                    {
                                        var machine = new VSCodeRemoteMachine
                                        {
                                            Host = h.Host,
                                            HostName = h.HostName ?? string.Empty,
                                            User = h.User ?? string.Empty
                                        };

                                        results.Add(machine);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var message = $"Failed to deserialize ${vscode_settings}";
                            Log.Exception(message, ex, GetType());
                        }
                    }
                }

                return results;
            }
        }
    }
}
