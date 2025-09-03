// Copyright (c) Artisense. All rights reserved.

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
                    Console.WriteLine($"📥 Global hook message: 0x{message:X4}");
                    
                    // Check if this might be from a pen/tablet
                    var extraInfo = GetMessageExtraInfo().ToInt64();
                    bool isPen = IsPenMessage(extraInfo);
                    
                    Console.WriteLine($"📊 Extra info: 0x{extraInfo:X8}, IsPen: {isPen}");
                    
                    switch (message)
                    {
                        case WM_LBUTTONDOWN:
                            Console.WriteLine("🖊️ Global: Left button down");
                            HandlePenDown(isPen);
                            break;
                            
                        case WM_LBUTTONUP:
                            Console.WriteLine("🖊️ Global: Left button up");
                            HandlePenUp();
                            break;
                            
                        case WM_MOUSEMOVE:
                            if (isInContact)
                            {
                                Console.WriteLine("✏️ Global: Mouse/pen move while in contact");
                                HandlePenMove(isPen);
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
            
            const long WACOM_SIGNATURE = 0xFF515700;
            const long SURFACE_SIGNATURE = 0xFF515701;
            const long GENERIC_PEN_BIT = 0x80000000;
            
            // Check for known pen signatures
            if ((extraInfo & 0xFFFFFF00) == WACOM_SIGNATURE ||
                (extraInfo & 0xFFFFFF00) == SURFACE_SIGNATURE ||
                (extraInfo & GENERIC_PEN_BIT) != 0)
            {
                return true;
            }
            
            // Some tablets use different patterns - log for debugging
            if (extraInfo != 0)
            {
                Console.WriteLine($"🔍 Unknown device signature: 0x{extraInfo:X8}");
            }
            
            return false;
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

        #endregion
    }
}
