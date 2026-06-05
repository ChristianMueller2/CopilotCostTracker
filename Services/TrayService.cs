// Services/TrayService.cs
using System.Drawing;
using System.Windows.Forms;

namespace CopilotCostTracker.Services;

public class TrayService : ITrayService, INotificationService, IDisposable
{
    private NotifyIcon?               _notifyIcon;
    private System.Threading.Thread?  _staThread;
    private SynchronizationContext?   _staContext;
    private readonly ManualResetEventSlim _ready = new(false);

    public void Initialize(Action onOpen, Action onQuit)
    {
        _staThread = new System.Threading.Thread(() =>
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            WindowsFormsSynchronizationContext.AutoInstall = false;
            var ctx = new WindowsFormsSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(ctx);
            _staContext = ctx;

            var icon = CreateIcon(System.Drawing.Color.FromArgb(139, 195, 74));
            _notifyIcon = new NotifyIcon
            {
                Icon    = icon,
                Text    = "CopilotCostTracker",
                Visible = true
            };

            var menu     = new ContextMenuStrip();
            var openItem = new ToolStripMenuItem("Öffnen");
            openItem.Click += (_, _) => onOpen();
            var sep      = new ToolStripSeparator();
            var quitItem = new ToolStripMenuItem("Beenden");
            quitItem.Click += (_, _) => onQuit();
            menu.Items.Add(openItem);
            menu.Items.Add(sep);
            menu.Items.Add(quitItem);
            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (_, _) => onOpen();

            _ready.Set();
            System.Windows.Forms.Application.Run();
        });
        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.IsBackground = true;
        _staThread.Start();
        _ready.Wait();
    }

    public void UpdateTooltip(string text)
    {
        if (_notifyIcon == null) return;
        var safe = text.Length > 63 ? text[..63] : text;
        _staContext?.Post(_ =>
        {
            if (_notifyIcon != null)
                _notifyIcon.Text = safe;
        }, null);
    }

    /// <summary>
    /// Redraws the tray icon. Pass dailyLimit = 0 to use neutral green.
    /// The icon turns yellow above 75 % of limit and red above 100 %.
    /// </summary>
    public void UpdateTrayIcon(decimal dailyCost, decimal dailyLimit)
        => UpdateTrayIconInternal(dailyCost, dailyLimit);

    private void UpdateTrayIconInternal(decimal dailyCost, decimal dailyLimit)
    {
        var color = dailyLimit <= 0
            ? System.Drawing.Color.FromArgb(139, 195, 74)   // green – no limit set
            : dailyCost >= dailyLimit
                ? System.Drawing.Color.FromArgb(255, 80, 80)   // red   – over limit
                : dailyCost >= dailyLimit * 0.75m
                    ? System.Drawing.Color.FromArgb(255, 200, 60) // amber – approaching
                    : System.Drawing.Color.FromArgb(139, 195, 74); // green – fine

        _staContext?.Post(_ =>
        {
            if (_notifyIcon == null) return;
            var oldIcon = _notifyIcon.Icon;
            _notifyIcon.Icon = CreateIcon(color);
            oldIcon?.Dispose();
        }, null);
    }

    /// <inheritdoc />
    public void Show(string title, string message, NotificationSeverity severity = NotificationSeverity.Info)
        => ShowBalloon(title, message, severity switch
        {
            NotificationSeverity.Warning => ToolTipIcon.Warning,
            NotificationSeverity.Error   => ToolTipIcon.Error,
            _                            => ToolTipIcon.Info
        });

    private void ShowBalloon(string title, string message, ToolTipIcon icon)
    {
        _staContext?.Post(_ =>
        {
            if (_notifyIcon == null) return;
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText  = message;
            _notifyIcon.BalloonTipIcon  = icon;
            _notifyIcon.ShowBalloonTip(6000);
        }, null);
    }

    private static System.Drawing.Icon CreateIcon(System.Drawing.Color textColor)
    {
        using var bmp  = new System.Drawing.Bitmap(16, 16);
        using var g    = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.FromArgb(30, 30, 30));
        using var font  = new System.Drawing.Font("Arial", 9f, System.Drawing.FontStyle.Bold);
        using var brush = new System.Drawing.SolidBrush(textColor);
        g.DrawString("$", font, brush, new System.Drawing.PointF(1, 1));
        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        System.Windows.Forms.Application.ExitThread();
    }
}
