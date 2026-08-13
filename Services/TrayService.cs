// Services/TrayService.cs
// Pure Win32 P/Invoke implementation – no WinForms or System.Drawing dependency,
// fully compatible with NativeAOT / PublishAot.
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CopilotCostTracker.Services;

public sealed class TrayService : ITrayService, INotificationService, IDisposable
{
    // ── Singleton ref used by the static WndProc ─────────────────────────────
    private static volatile TrayService? _instance;

    // ── Thread state ─────────────────────────────────────────────────────────
    private Thread?  _thread;
    private IntPtr   _hWnd  = IntPtr.Zero;
    private IntPtr   _hIcon = IntPtr.Zero;
    private readonly ManualResetEventSlim _ready = new(false);

    // ── Cross-thread work queue ───────────────────────────────────────────────
    private readonly ConcurrentQueue<Action> _queue = new();

    // ── Callbacks set by Initialize ──────────────────────────────────────────
    private Action _onOpen = static () => { };
    private Action _onQuit = static () => { };

    // ─────────────────────────────────────────────────────────────────────────
    // Win32 Constants
    // ─────────────────────────────────────────────────────────────────────────
    private const uint WM_TRAYICON   = 0x8001u; // WM_APP + 1
    private const uint WM_DISPATCH   = 0x8002u; // WM_APP + 2  – drain _queue
    private const uint WM_TRAY_QUIT  = 0x8003u; // WM_APP + 3  – clean shutdown
    private const uint WM_LBUTTONDBLCLK = 0x0203u;
    private const uint WM_RBUTTONUP     = 0x0205u;
    private const uint NIM_ADD      = 0u;
    private const uint NIM_MODIFY   = 1u;
    private const uint NIM_DELETE   = 2u;
    private const uint NIF_MESSAGE  = 0x01u;
    private const uint NIF_ICON     = 0x02u;
    private const uint NIF_TIP      = 0x04u;
    private const uint NIF_INFO     = 0x10u;
    private const uint NIIF_INFO    = 1u;
    private const uint NIIF_WARNING = 2u;
    private const uint NIIF_ERROR   = 3u;
    private const int  TRANSPARENT  = 1;   // SetBkMode
    private const int  FW_BOLD      = 700;
    private const uint MF_STRING    = 0x00u;
    private const uint MF_SEPARATOR = 0x800u;
    private const uint TPM_RIGHTBUTTON = 0x0002u;
    private const uint TPM_RETURNCMD   = 0x0100u;
    private const uint TPM_NONOTIFY    = 0x0080u;
    private const uint CMD_OPEN = 1u;
    private const uint CMD_QUIT = 2u;
    private const string WndClassName = "CctTray";
    private static readonly IntPtr WndClassNamePtr = Marshal.StringToHGlobalUni(WndClassName);

    // ─────────────────────────────────────────────────────────────────────────
    // Native structs
    // ─────────────────────────────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT  { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint   message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint   time;
        public POINT  pt;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private unsafe struct WNDCLASSEXW
    {
        public uint   cbSize;
        public uint   style;
        public delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, IntPtr, IntPtr> lpfnWndProc;
        public int    cbClsExtra;
        public int    cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public IntPtr lpszMenuName;
        public IntPtr lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATAW
    {
        public uint   cbSize;
        public IntPtr hWnd;
        public uint   uID;
        public uint   uFlags;
        public uint   uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint   dwState;
        public uint   dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint   uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint   dwInfoFlags;
        public Guid   guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        public int    fIcon;
        public uint   xHotspot;
        public uint   yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    private static readonly uint NidSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>();

    // ─────────────────────────────────────────────────────────────────────────
    // P/Invoke declarations
    // ─────────────────────────────────────────────────────────────────────────
    [DllImport("Shell32.dll")]
    private static extern bool Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATAW lpData);

    [DllImport("User32.dll", CharSet = CharSet.Unicode)]
    private static extern unsafe ushort RegisterClassExW(WNDCLASSEXW* lpWndClass);

    [DllImport("User32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowExW(uint dwExStyle, string lpClassName,
        string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("User32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("User32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("User32.dll")]
    private static extern int GetMessageW(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("User32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("User32.dll")]
    private static extern IntPtr DispatchMessageW(ref MSG lpMsg);

    [DllImport("User32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("User32.dll")]
    private static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("User32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("User32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("User32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("User32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenuW(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string? lpNewItem);

    [DllImport("User32.dll")]
    private static extern uint TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y,
        int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("User32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("User32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("User32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("User32.dll")]
    private static extern int FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);

    [DllImport("User32.dll")]
    private static extern int SetBkMode(IntPtr hdc, int iBkMode);

    [DllImport("User32.dll")]
    private static extern uint SetTextColor(IntPtr hdc, uint color);

    [DllImport("User32.dll", CharSet = CharSet.Unicode)]
    private static extern bool TextOutW(IntPtr hdc, int x, int y, string lpString, int c);

    [DllImport("User32.dll")]
    private static extern IntPtr CreateIconIndirect(ref ICONINFO piconinfo);

    [DllImport("User32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string? lpModuleName);

    [DllImport("Gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("Gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("Gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

    [DllImport("Gdi32.dll")]
    private static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint nPlanes,
        uint nBitCount, IntPtr lpBits);

    [DllImport("Gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("Gdi32.dll")]
    private static extern bool DeleteObject(IntPtr ho);

    [DllImport("Gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(uint color);

    [DllImport("Gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFontW(int cHeight, int cWidth, int cEscapement,
        int cOrientation, int cWeight, uint bItalic, uint bUnderline, uint bStrikeOut,
        uint iCharSet, uint iOutPrecision, uint iClipPrecision, uint iQuality,
        uint iPitchAndFamily, string? pszFaceName);

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    public void Initialize(Action onOpen, Action onQuit)
    {
        _onOpen   = onOpen;
        _onQuit   = onQuit;
        _instance = this;

        _thread = new Thread(ThreadProc) { IsBackground = true };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _ready.Wait();
    }

    /// <inheritdoc />
    public void UpdateTooltip(string text)
    {
        var safe = text.Length > 63 ? text[..63] : text;
        PostTrayAction(() => SetTip(safe));
    }

    /// <summary>
    /// Redraws the tray icon. Pass dailyLimit = 0 to use neutral green.
    /// The icon turns yellow above 75 % of limit and red above 100 %.
    /// </summary>
    public void UpdateTrayIcon(decimal dailyCost, decimal dailyLimit)
    {
        var (r, g, b) = dailyLimit <= 0
            ? (139, 195, 74)  // green  – no limit set
            : dailyCost >= dailyLimit
                ? (255, 80,  80)  // red   – over limit
                : dailyCost >= dailyLimit * 0.75m
                    ? (255, 200, 60)  // amber – approaching
                    : (139, 195, 74); // green – fine

        PostTrayAction(() => SetIcon(r, g, b));
    }

    /// <inheritdoc />
    public void Show(string title, string message, NotificationSeverity severity = NotificationSeverity.Info)
    {
        var flags = severity switch
        {
            NotificationSeverity.Warning => NIIF_WARNING,
            NotificationSeverity.Error   => NIIF_ERROR,
            _                            => NIIF_INFO
        };
        PostTrayAction(() => ShowBalloon(title, message, flags));
    }

    public void Dispose()
    {
        var thread = _thread;
        if (_hWnd != IntPtr.Zero)
            PostMessageW(_hWnd, WM_TRAY_QUIT, IntPtr.Zero, IntPtr.Zero);
        thread?.Join();
        _thread = null;
        _instance = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Message thread
    // ─────────────────────────────────────────────────────────────────────────

    private void ThreadProc()
    {
        RegisterWindowClass();

        _hWnd = CreateWindowExW(0, WndClassName, string.Empty, 0, 0, 0, 0, 0,
            new IntPtr(-3) /* HWND_MESSAGE */, IntPtr.Zero, GetModuleHandleW(null), IntPtr.Zero);

        if (_hWnd == IntPtr.Zero) { _ready.Set(); return; }

        _hIcon = CreateHIcon(139, 195, 74); // initial green

        var nid = MakeNid();
        nid.uFlags           = NIF_MESSAGE | NIF_ICON | NIF_TIP;
        nid.uCallbackMessage = WM_TRAYICON;
        nid.hIcon            = _hIcon;
        nid.szTip            = "CopilotCostTracker";
        Shell_NotifyIconW(NIM_ADD, ref nid);

        _ready.Set();

        while (GetMessageW(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static IntPtr WndProcStatic(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (_instance is { } svc)
            return svc.HandleMessage(hWnd, msg, wParam, lParam);
        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private IntPtr HandleMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_TRAYICON:
                var mouseMsg = (uint)(lParam.ToInt64() & 0xFFFF);
                if (mouseMsg == WM_LBUTTONDBLCLK)
                    _onOpen();
                else if (mouseMsg == WM_RBUTTONUP)
                    ShowContextMenu(hWnd);
                return IntPtr.Zero;

            case WM_DISPATCH:
                while (_queue.TryDequeue(out var act)) act();
                return IntPtr.Zero;

            case WM_TRAY_QUIT:
                var nid = MakeNid();
                Shell_NotifyIconW(NIM_DELETE, ref nid);
                if (_hIcon != IntPtr.Zero) { DestroyIcon(_hIcon); _hIcon = IntPtr.Zero; }
                DestroyWindow(hWnd);
                _hWnd = IntPtr.Zero;
                PostQuitMessage(0);
                return IntPtr.Zero;
        }
        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private void ShowContextMenu(IntPtr hWnd)
    {
        var hMenu = CreatePopupMenu();
        AppendMenuW(hMenu, MF_STRING,    new IntPtr(CMD_OPEN), "Öffnen");
        AppendMenuW(hMenu, MF_SEPARATOR, IntPtr.Zero,          null);
        AppendMenuW(hMenu, MF_STRING,    new IntPtr(CMD_QUIT), "Beenden");

        GetCursorPos(out var pt);
        SetForegroundWindow(hWnd);
        var cmd = TrackPopupMenu(hMenu,
            TPM_RIGHTBUTTON | TPM_RETURNCMD | TPM_NONOTIFY,
            pt.x, pt.y, 0, hWnd, IntPtr.Zero);
        DestroyMenu(hMenu);

        if      (cmd == CMD_OPEN) _onOpen();
        else if (cmd == CMD_QUIT) _onQuit();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void PostTrayAction(Action action)
    {
        _queue.Enqueue(action);
        if (_hWnd != IntPtr.Zero)
            PostMessageW(_hWnd, WM_DISPATCH, IntPtr.Zero, IntPtr.Zero);
    }

    private void SetTip(string tip)
    {
        var nid = MakeNid();
        nid.uFlags = NIF_TIP;
        nid.szTip  = tip;
        Shell_NotifyIconW(NIM_MODIFY, ref nid);
    }

    private void SetIcon(int r, int g, int b)
    {
        var newIcon = CreateHIcon(r, g, b);
        var nid = MakeNid();
        nid.uFlags = NIF_ICON;
        nid.hIcon  = newIcon;
        Shell_NotifyIconW(NIM_MODIFY, ref nid);
        var old = _hIcon;
        _hIcon = newIcon;
        if (old != IntPtr.Zero) DestroyIcon(old);
    }

    private void ShowBalloon(string title, string message, uint flags)
    {
        var nid = MakeNid();
        nid.uFlags            = NIF_INFO;
        nid.szInfoTitle       = title.Length   > 63  ? title[..63]     : title;
        nid.szInfo            = message.Length > 255 ? message[..255]  : message;
        nid.dwInfoFlags       = flags;
        nid.uTimeoutOrVersion = 6000;
        Shell_NotifyIconW(NIM_MODIFY, ref nid);
    }

    private NOTIFYICONDATAW MakeNid() => new()
    {
        cbSize      = NidSize,
        hWnd        = _hWnd,
        uID         = 1,
        szTip       = string.Empty,
        szInfo      = string.Empty,
        szInfoTitle = string.Empty,
    };

    private unsafe void RegisterWindowClass()
    {
        var wc = new WNDCLASSEXW
        {
            cbSize        = (uint)sizeof(WNDCLASSEXW),
            lpfnWndProc   = &WndProcStatic,
            hInstance     = GetModuleHandleW(null),
            lpszClassName = WndClassNamePtr,
        };
        RegisterClassExW(&wc);
    }

    /// <summary>
    /// Creates a 16×16 tray icon with a "$" character in <paramref name="r"/>,
    /// <paramref name="g"/>, <paramref name="b"/> color using pure GDI32 calls.
    /// </summary>
    private static IntPtr CreateHIcon(int r, int g, int b)
    {
        // Win32 COLORREF is 0x00BBGGRR
        uint bgColor = 0x00_1E_1E_1Eu;
        uint fgColor = (uint)((b << 16) | (g << 8) | r);

        IntPtr screenDC = GetDC(IntPtr.Zero);
        IntPtr memDC    = CreateCompatibleDC(screenDC);
        IntPtr hbm      = CreateCompatibleBitmap(screenDC, 16, 16);
        ReleaseDC(IntPtr.Zero, screenDC);

        IntPtr oldBmp = SelectObject(memDC, hbm);

        // Fill dark background
        IntPtr bgBrush = CreateSolidBrush(bgColor);
        var rect = new RECT { left = 0, top = 0, right = 16, bottom = 16 };
        FillRect(memDC, ref rect, bgBrush);
        DeleteObject(bgBrush);

        // Draw "$" in the requested color
        SetBkMode(memDC, TRANSPARENT);
        SetTextColor(memDC, fgColor);
        IntPtr hFont   = CreateFontW(12, 0, 0, 0, FW_BOLD, 0, 0, 0, 0, 0, 0, 0, 0, "Arial");
        IntPtr oldFont = SelectObject(memDC, hFont);
        TextOutW(memDC, 2, 1, "$", 1);
        SelectObject(memDC, oldFont);
        DeleteObject(hFont);

        SelectObject(memDC, oldBmp);
        DeleteDC(memDC);

        // 1bpp all-zero mask: AND=0 means XOR-color is used (fully opaque)
        // 16×16 monochrome bitmap: 16 bits/row padded to DWORD = 4 bytes × 16 rows = 64 bytes
        IntPtr maskMem = Marshal.AllocHGlobal(64);
        Marshal.Copy(new byte[64], 0, maskMem, 64);
        IntPtr hbmMask = CreateBitmap(16, 16, 1, 1, maskMem);
        Marshal.FreeHGlobal(maskMem);

        var ii = new ICONINFO { fIcon = 1, hbmColor = hbm, hbmMask = hbmMask };
        IntPtr hIcon = CreateIconIndirect(ref ii);

        DeleteObject(hbm);
        DeleteObject(hbmMask);
        return hIcon;
    }
}
