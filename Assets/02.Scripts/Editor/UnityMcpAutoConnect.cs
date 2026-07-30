#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class UnityMcpAutoConnect
{
    private const string AutoStartPreference = "MCPForUnity.AutoStartOnLoad";
    private const string ServerUrl = "http://127.0.0.1:8080";

    static UnityMcpAutoConnect()
    {
        Configure();
        EditorApplication.delayCall += Connect;
    }

    private static void Configure()
    {
        EditorPrefs.SetBool(AutoStartPreference, true);

        EditorConfigurationCache configuration = EditorConfigurationCache.Instance;
        configuration.SetUseHttpTransport(true);
        configuration.SetHttpTransportScope("local");
        configuration.SetHttpBaseUrl(ServerUrl);

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string wingetUvx = Path.Combine(
            localAppData,
            "Microsoft",
            "WinGet",
            "Packages",
            "astral-sh.uv_Microsoft.Winget.Source_8wekyb3d8bbwe",
            "uvx.exe");

        if (File.Exists(wingetUvx))
        {
            configuration.SetUvxPathOverride(wingetUvx);
        }
    }

    private static async void Connect()
    {
        try
        {
            IServerManagementService server = MCPServiceLocator.Server;

            if (!server.IsLocalHttpServerReachable() &&
                !server.StartLocalHttpServer(quiet: true))
            {
                Debug.LogWarning("[Unity MCP] Could not start the local HTTP server.");
                return;
            }

            DateTime deadline = DateTime.UtcNow.AddSeconds(60);
            while (!server.IsLocalHttpServerReachable() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(500);
            }

            if (!server.IsLocalHttpServerReachable())
            {
                Debug.LogWarning("[Unity MCP] HTTP server did not become ready within 60 seconds.");
                return;
            }

            if (MCPServiceLocator.Bridge.IsRunning)
            {
                return;
            }

            bool connected = await MCPServiceLocator.Bridge.StartAsync();
            if (connected)
            {
                Debug.Log("[Unity MCP] Connected to the shared Codex HTTP server.");
            }
            else
            {
                Debug.LogWarning("[Unity MCP] The HTTP server is running, but the Editor bridge could not connect.");
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
#endif
