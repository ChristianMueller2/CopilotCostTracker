// Services/TrayService.cs
using System.Drawing;
using System.Windows.Forms;

namespace CopilotCostTracker.Services;

public class TrayService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private System.Threading.Thread? _staThread;
    private readonly ManualResetEventSlim _ready = new(false);

    public void Initialize(Action onOpen, Action onQuit)
    {
        _staThread = new System.Threading.Thread(() =>
        {
            System.Windows.Forms.Application.EnableVisualStyles();

            var icon = CreateIcon();
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
        // NotifyIcon.Text has a 64-char limit
        _notifyIcon.Text = text.Length > 63 ? text[..63] : text;
    }

    private static System.Drawing.Icon CreateIcon()
    {
        using var bmp  = new System.Drawing.Bitmap(16, 16);
        using var g    = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.FromArgb(30, 30, 30));
        using var font  = new System.Drawing.Font("Arial", 9f, System.Drawing.FontStyle.Bold);
        using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(139, 195, 74));
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
