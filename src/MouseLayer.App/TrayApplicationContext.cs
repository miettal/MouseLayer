using MouseLayer.Core;
using MouseLayer.Core.Profiles;

namespace MouseLayer.App;

/// <summary>Runs MouseLayer with no visible window: a tray icon plus the background remap engine.</summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly MouseLayerEngine _engine;
    private readonly string _configPath;

    public TrayApplicationContext()
    {
        _configPath = ConfigStore.GetDefaultConfigPath();
        var config = ConfigStore.Load(_configPath);

        _engine = new MouseLayerEngine(config);
        _engine.Start();

        _trayIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "MouseLayer",
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open config folder", null, (_, _) =>
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_configPath}\""));
        menu.Items.Add("Reload config", null, (_, _) =>
            _engine.Config = ConfigStore.Load(_configPath));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());
        return menu;
    }

    private void ExitApplication()
    {
        _trayIcon.Visible = false;
        _engine.Dispose();
        Application.Exit();
    }
}
