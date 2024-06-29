// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Community.PowerToys.Run.Plugin.VSCodeWorkspaces.WorkspacesHelper
{
    public class VSCodeWorkspace
    {
        public string Path { get; set; }

        public string RelativePath { get; set; }

        public string FolderName { get; set; }

        public string ExtraInfo { get; set; }

        public WorkspaceEnvironment WorkspaceEnvironment { get; set; }

        public WorkspaceType WorkspaceType { get; set; }

        

        public string WorkspaceEnvironmentToString()
        {
            return WorkspaceEnvironment switch
            {
                WorkspaceEnvironment.Local => "Local",
                WorkspaceEnvironment.Codespaces => "Codespaces",
                WorkspaceEnvironment.RemoteSSH => "SSH",
                WorkspaceEnvironment.RemoteWSL => "WSL",
                WorkspaceEnvironment.DevContainer => "DevContainer",
                WorkspaceEnvironment.RemoteTunnel => "Tunnel",
                _ => string.Empty,
            };
        }
    }

    public enum WorkspaceEnvironment
    {
        Local = 1,
        Codespaces = 2,
        RemoteWSL = 3,
        RemoteSSH = 4,
        DevContainer = 5,
        RemoteTunnel = 6,
    }

    public enum WorkspaceType
    {
        ProjectFolder = 1,
        WorkspaceFile = 2,
    }
}
