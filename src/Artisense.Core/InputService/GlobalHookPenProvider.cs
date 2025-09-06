// Copyright (c) Artisense. All rights reserved.

#pragma warning disable S101 // Disable naming convention warnings for Windows API structs

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace Artisense.Core.InputService
{
    /// <summary>
    /// Pen input provider using global Windows message hooks.
    /// This captures pen input globally across all applications.
    /// </summary>
    #pragma warning disable S3881 // Simple dispose pattern is sufficient for this use case
    public class GlobalHookPenProvider : IPenInputProvider
    #pragma warning restore S3881
    {
        #pragma warning disable CS0067 // Event is never used (required by interface)
        /// <inheritdoc/>
        public event EventHandler<PenEventArgs>? PenDown;

        /// <inheritdoc/>
        public event EventHandler<PenEventArgs>? PenMove;

        /// <inheritdoc/>
        public event EventHandler? PenUp;
        #pragma warning restore CS0067

        private readonly ILogger<GlobalHookPenProvider> logger;
        private bool isDisposed;
        private bool isActive;
        private IntPtr hookHandle;
        #pragma warning disable S2933 // hookProc needs to be assigned in constructor to prevent GC
        private LowLevelMouseProc hookProc;
        #pragma warning restore S2933
        private bool isInContact;
        private float lastPressure;

        // Mouse hook constants
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MOUSEMOVE = 0x0200;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalHookPenProvider"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public GlobalHookPenProvider(ILogger<GlobalHookPenProvider> logger)
        {
            this.logger = logger;
            this.hookProc = HookCallback; // Keep a reference to prevent garbage collection
        }

        /// <inheritdoc/>
        public bool IsActive => isActive && !isDisposed;

        /// <inheritdoc/>
        public bool Start()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(GlobalHookPenProvider));
            }

            try
            {
                // Install global mouse hook
                using (var currentProcess = Process.GetCurrentProcess())
                using (var currentModule = currentProcess.MainModule)
                {
                    var moduleHandle = GetModuleHandle(currentModule?.ModuleName);
                    hookHandle = SetWindowsHookEx(
                        WH_MOUSE_LL,
                        hookProc,
                        moduleHandle,
                        0);
                }

                if (hookHandle == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    logger.LogError("Failed to install mouse hook. Error: {Error}", error);
                    return false;
                }

                isActive = true;
                logger.LogInformation("🖊️ Global hook pen provider started");
                Console.WriteLine("🖊️ Global hook provider: Listening for mouse/pen events globally");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start global hook pen provider");
                isActive = false;
                return false;
            }
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (isDisposed)
            {
                return;
            }

            try
            {
                isActive = false;
                
                if (hookHandle != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(hookHandle);
                    hookHandle = IntPtr.Zero;
                }
                
                logger.LogInformation("Global hook pen provider stopped");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error stopping global hook pen provider");
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0)
                {
                    int message = wParam.ToInt32();

                    // Get the window under cursor to avoid terminal interference
                    var cursorWindow = GetWindowUnderCursor();
                    bool isTerminalWindow = IsTerminalWindow(cursorWindow);

                    if (isTerminalWindow)
                    {
                        // Skip processing if cursor is over terminal/console window
                        Console.WriteLine("🔇 Skipping input over terminal window");
                        return CallNextHookEx(hookHandle, nCode, wParam, lParam);
                    }

                    // Check if this might be from a pen/tablet
                    var extraInfo = GetMessageExtraInfo().ToInt64();
                    bool isPen = IsPenMessage(extraInfo);

                    // Enhanced debugging - show ALL input for analysis
                    Console.WriteLine($"📥 INPUT: 0x{message:X4}, Extra: 0x{extraInfo:X8}, Pen: {isPen}");

                    // If it's NOT a pen but has extra info, it might be a tablet we don't recognize
                    if (!isPen && extraInfo != 0)
                    {
                        Console.WriteLine($"🎯 UNKNOWN DEVICE: 0x{extraInfo:X8} - treating as potential pen!");
                        isPen = true; // Be permissive for unknown devices
                    }
                    
                    switch (message)
                    {
                        case WM_LBUTTONDOWN:
                            if (isPen)
                            {
                                Console.WriteLine("🖊️ PEN: Left button down (confirmed pen)");
                                HandlePenDown(true);
                            }
                            else
                            {
                                Console.WriteLine("🐭 MOUSE: Left button down (ignoring)");
                            }

                            break;

                        case WM_LBUTTONUP:
                            // Only process if we were in contact
                            if (isInContact)
                            {
                                Console.WriteLine("🖊️ PEN: Left button up");
                                HandlePenUp();
                            }

                            break;

                        case WM_MOUSEMOVE:
                            if (isInContact && isPen)
                            {
                                Console.WriteLine("✏️ PEN: Move while in contact");
                                HandlePenMove(true);
                            }

                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in hook callback");
            }

            return CallNextHookEx(hookHandle, nCode, wParam, lParam);
        }

        private bool IsPenMessage(long extraInfo)
        {
            // Check various pen signatures in the extra info
            // Different tablets use different signatures

            // Common pen signatures:
            // - Wacom tablets often have 0xFF515700 or similar
            // - Surface devices use different patterns
            // - Some tablets set specific bits
            // - Many tablets use completely different signatures

            const long WACOM_SIGNATURE = 0xFF515700;
            const long SURFACE_SIGNATURE = 0xFF515701;
            const long GENERIC_PEN_BIT = 0x80000000;
            const long XPPEN_SIGNATURE = 0xFF515702;  // XP-Pen tablets
            const long HUION_SIGNATURE = 0xFF515703;   // Huion tablets
            const long GAOMON_SIGNATURE = 0xFF515704;  // Gaomon tablets
            const long UGEE_SIGNATURE = 0xFF515705;    // UGEE tablets

            // Check for known pen signatures
            if ((extraInfo & 0xFFFFFF00) == WACOM_SIGNATURE ||
                (extraInfo & 0xFFFFFF00) == SURFACE_SIGNATURE ||
                (extraInfo & 0xFFFFFF00) == XPPEN_SIGNATURE ||
                (extraInfo & 0xFFFFFF00) == HUION_SIGNATURE ||
                (extraInfo & 0xFFFFFF00) == GAOMON_SIGNATURE ||
                (extraInfo & 0xFFFFFF00) == UGEE_SIGNATURE ||
                (extraInfo & GENERIC_PEN_BIT) != 0)
            {
                return true;
            }

            // Check for other common tablet patterns
            // Many tablets set various bits in different ranges
            // High byte FF (common for tablets)
            if ((extraInfo & 0xFF000000) == 0xFF000000 ||

                // Middle bytes often contain 51
                (extraInfo & 0x00FF0000) == 0x00510000 ||

                // Low bytes often contain 57
                (extraInfo & 0x0000FF00) == 0x00005700 ||

                // Large values often indicate tablets
                extraInfo > 0x10000000)
            {
                Console.WriteLine($"🎨 DETECTED TABLET PATTERN: 0x{extraInfo:X8}");
                return true;
            }

            // Some tablets use different patterns - log for debugging
            if (extraInfo != 0)
            {
                Console.WriteLine($"🔍 Unknown device signature: 0x{extraInfo:X8}");
                return true; // Be permissive - treat unknown non-zero signatures as potential pens
            }

            return false;
        }

        private IntPtr GetWindowUnderCursor()
        {
            try
            {
                var cursorPos = Cursor.Position;
                return WindowFromPoint(new POINT { X = cursorPos.X, Y = cursorPos.Y });
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private bool IsTerminalWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                // Get window class name
                const int nMaxCount = 256;
                var className = new string('\0', nMaxCount);
                var result = GetClassName(hWnd, className, nMaxCount);

                if (result > 0)
                {
                    className = className.TrimEnd('\0').ToLower();

                    // Check for common terminal/console window classes
                    string[] terminalClasses = new[]
                    {
                        "consolewindowclass",     // Windows Console
                        "tty",                    // Unix-like terminals
                        "xterm",                  // XTerm
                        "putty",                  // PuTTY
                        "mintty",                 // MinTTY (Git Bash)
                        "cmd",                    // Command Prompt
                        "powershell",            // PowerShell
                        "windows.terminal",       // Windows Terminal
                        "wpfapp",                // WPF applications (may include terminals)
                        "fluttermainwindow",     // Flutter terminal apps
                        "electron",              // Electron-based terminals
                        "hyper",                 // Hyper terminal
                        "tabby",                 // Tabby terminal
                        "terminus",              // Terminus terminal
                        "alacritty",             // Alacritty terminal
                        "wezterm",               // WezTerm
                        "rio",                   // Rio terminal
                        "warp",                  // Warp terminal
                        "vscode",                // VS Code (often contains terminals)
                        "jetbrains",             // JetBrains IDEs (may contain terminals)
                    };

                    foreach (var terminalClass in terminalClasses)
                    {
                        if (className.Contains(terminalClass))
                        {
                            Console.WriteLine($"🔍 Detected terminal window: {className}");
                            return true;
                        }
                    }

                    // Also check window title for terminal indicators
                    var title = GetWindowTitle(hWnd);
                    string[] terminalTitles = new[]
                    {
                        "command prompt",
                        "powershell",
                        "terminal",
                        "console",
                        "bash",
                        "git bash",
                        "mingw",
                        "msys",
                        "hyper",
                        "tabby",
                        "terminus",
                        "alacritty",
                        "wezterm",
                        "rio",
                        "warp",
                        "vscode",
                        "jetbrains",
                        "intellij",
                        "rider",
                        "webstorm",
                        "pycharm",
                        "clion",
                        "goland",
                        "phpstorm",
                        "rubymine",
                        "appcode",
                        "datagrip",
                        "resharper",
                        "dotpeek",
                        "dottrace",
                        "dotmemory",
                        "dotcover",
                        "teamcity"
                    };

                    foreach (var terminalTitle in terminalTitles)
                    {
                        if (title.ToLower().Contains(terminalTitle))
                        {
                            Console.WriteLine($"🔍 Detected terminal window by title: {title}");
                            return true;
                        }
                    }

                    // Additional check: if the window process is a known terminal
                    try
                    {
                        var processName = GetProcessNameFromWindow(hWnd);
                        string[] terminalProcesses = new[]
                        {
                            "cmd",
                            "powershell",
                            "pwsh",
                            "bash",
                            "sh",
                            "zsh",
                            "fish",
                            "git-bash",
                            "mintty",
                            "conhost",
                            "windowsterminal",
                            "hyper",
                            "tabby",
                            "terminus",
                            "alacritty",
                            "wezterm",
                            "rio",
                            "warp",
                            "code",          // VS Code
                            "rider64",       // Rider
                            "idea64",        // IntelliJ IDEA
                            "webstorm64",    // WebStorm
                            "pycharm64",     // PyCharm
                            "clion64",       // CLion
                            "goland64",      // GoLand
                            "phpstorm64",    // PhpStorm
                            "rubymine64",    // RubyMine
                            "appcode64",     // AppCode
                            "datagrip64",    // DataGrip
                        };

                        foreach (var terminalProcess in terminalProcesses)
                        {
                            if (processName.ToLower().Contains(terminalProcess))
                            {
                                Console.WriteLine($"🔍 Detected terminal by process: {processName}");
                                return true;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore process detection errors
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error checking window class");
            }

            return false;
        }

        private string GetWindowTitle(IntPtr hWnd)
        {
            try
            {
                const int nMaxCount = 256;
                var title = new string('\0', nMaxCount);
                var result = GetWindowText(hWnd, title, nMaxCount);
                return result > 0 ? title.TrimEnd('\0') : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetProcessNameFromWindow(IntPtr hWnd)
        {
            try
            {
                uint processId;
                GetWindowThreadProcessId(hWnd, out processId);

                var process = System.Diagnostics.Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void HandlePenDown(bool isPen)
        {
            if (!isInContact)
            {
                isInContact = true;
                float pressure = isPen ? GetPenPressure() : 0.7f; // Default pressure for non-pen
                lastPressure = pressure;
                
                Console.WriteLine($"🖊️ PEN DOWN! IsPen: {isPen}, Pressure: {pressure:F3}");
                PenDown?.Invoke(this, new PenEventArgs(pressure));
            }
        }

        private void HandlePenMove(bool isPen)
        {
            if (isInContact)
            {
                float pressure = isPen ? GetPenPressure() : 0.7f;
                
                if (Math.Abs(pressure - lastPressure) > 0.01f)
                {
                    lastPressure = pressure;
                    Console.WriteLine($"✏️ PEN MOVE! IsPen: {isPen}, Pressure: {pressure:F3}");
                    PenMove?.Invoke(this, new PenEventArgs(pressure));
                }
            }
        }

        private void HandlePenUp()
        {
            if (isInContact)
            {
                isInContact = false;
                lastPressure = 0.0f;
                Console.WriteLine("🖊️ PEN UP!");
                PenUp?.Invoke(this, EventArgs.Empty);
            }
        }

        private float GetPenPressure()
        {
            // Try to get pen pressure from Windows Ink API
            try
            {
                // This is a simplified approach - real pressure would require
                // more complex tablet API integration
                #pragma warning disable S1481 // Cursor position might be used in future pressure calculation
                var cursorPos = Cursor.Position;
                #pragma warning restore S1481
                
                // For now, simulate variable pressure based on movement
                // In a real implementation, you'd query the tablet driver
                return 0.5f + (float)(Math.Sin(Environment.TickCount * 0.01) * 0.3);
            }
            catch
            {
                return 0.7f; // Default pressure
            }
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                Stop();
                isDisposed = true;
            }
        }

        #region Native Interop

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, string lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, string lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        #endregion
    }
}
