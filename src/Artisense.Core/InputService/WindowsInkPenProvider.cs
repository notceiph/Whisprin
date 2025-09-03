// Copyright (c) Artisense. All rights reserved.

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace Artisense.Core.InputService
{
    /// <summary>
    /// Pen input provider using Windows Ink/Pointer API.
    /// This works with most modern tablet devices that support Windows Ink.
    /// </summary>
    public class WindowsInkPenProvider : Form, IPenInputProvider
    {
        #pragma warning disable CS0067 // Event is never used (required by interface)
        /// <inheritdoc/>
        public event EventHandler<PenEventArgs>? PenDown;

        /// <inheritdoc/>
        public event EventHandler<PenEventArgs>? PenMove;

        /// <inheritdoc/>
        public event EventHandler? PenUp;
        #pragma warning restore CS0067

        private readonly ILogger<WindowsInkPenProvider> logger;
        private bool isDisposed;
        private bool isInContact;
        private float lastPressure;
        private bool isActive;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsInkPenProvider"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public WindowsInkPenProvider(ILogger<WindowsInkPenProvider> logger)
        {
            this.logger = logger;
            
            // Configure the form as a hidden message window
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            Visible = false;
            
            // Make it a tiny window
            Size = new System.Drawing.Size(1, 1);
            Location = new System.Drawing.Point(-10, -10);
        }

        /// <inheritdoc/>
        public bool IsActive => isActive && !isDisposed;

        /// <inheritdoc/>
        public bool Start()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(WindowsInkPenProvider));
            }

            try
            {
                // Create the window handle
                CreateHandle();
                isActive = true;
                
                logger.LogInformation("🖊️ Windows Ink pen provider started");
                Console.WriteLine("🖊️ Windows Ink provider: Listening for pen events");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start Windows Ink pen provider");
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
                if (IsHandleCreated)
                {
                    DestroyHandle();
                }
                
                logger.LogInformation("Windows Ink pen provider stopped");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error stopping Windows Ink pen provider");
            }
        }

        /// <summary>
        /// Process Windows messages for pen input.
        /// </summary>
        /// <param name="m">Windows message.</param>
        protected override void WndProc(ref Message m)
        {
            const int WM_POINTERDOWN = 0x0246;
            const int WM_POINTERUPDATE = 0x0245;
            const int WM_POINTERUP = 0x0247;

            try
            {
                switch (m.Msg)
                {
                    case WM_POINTERDOWN:
                        Console.WriteLine("📥 WM_POINTERDOWN received");
                        HandlePointerEvent(m.WParam, m.LParam, isDown: true);
                        break;
                        
                    case WM_POINTERUPDATE:
                        Console.WriteLine("📥 WM_POINTERUPDATE received");
                        HandlePointerEvent(m.WParam, m.LParam, isDown: null);
                        break;
                        
                    case WM_POINTERUP:
                        Console.WriteLine("📥 WM_POINTERUP received");
                        HandlePointerEvent(m.WParam, m.LParam, isDown: false);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing pointer message");
            }

            base.WndProc(ref m);
        }

        #pragma warning disable S1172 // Remove unused parameters
        private void HandlePointerEvent(IntPtr wParam, IntPtr lParam, bool? isDown)
        #pragma warning restore S1172
        {
            try
            {
                uint pointerId = (uint)(wParam.ToInt64() & 0xFFFF);
                
                // Get pointer info
                if (GetPointerInfo(pointerId, out PointerInfo pointerInfo))
                {
                    Console.WriteLine($"📊 Pointer info: Type={pointerInfo.pointerType}, Flags=0x{pointerInfo.pointerFlags:X}");
                    
                    // Check if this is a pen
                    if (pointerInfo.pointerType == PointerInputType.PT_PEN)
                    {
                        // Get pen-specific info with pressure
                        if (GetPointerPenInfo(pointerId, out PointerPenInfo penInfo))
                        {
                            float pressure = penInfo.pressure / 1024.0f; // Normalize pressure
                            bool inContact = (pointerInfo.pointerFlags & PointerFlags.INCONTACT) != 0;
                            
                            Console.WriteLine($"🖊️ Pen event: Contact={inContact}, Pressure={pressure:F3}");
                            
                            ProcessPenState(inContact, pressure);
                        }
                        else
                        {
                            // Fallback: use contact state without pressure
                            bool inContact = (pointerInfo.pointerFlags & PointerFlags.INCONTACT) != 0;
                            Console.WriteLine($"🖊️ Pen event (no pressure): Contact={inContact}");
                            ProcessPenState(inContact, 0.5f); // Default pressure
                        }
                    }
                    else if (pointerInfo.pointerType == PointerInputType.PT_TOUCH)
                    {
                        // Handle touch as pen for testing
                        bool inContact = (pointerInfo.pointerFlags & PointerFlags.INCONTACT) != 0;
                        Console.WriteLine($"👆 Touch event treated as pen: Contact={inContact}");
                        ProcessPenState(inContact, 0.7f); // Fixed pressure for touch
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Failed to get pointer info for ID {pointerId}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling pointer event");
            }
        }

        private void ProcessPenState(bool inContact, float pressure)
        {
            if (inContact && !isInContact)
            {
                // Pen down
                isInContact = true;
                lastPressure = pressure;
                Console.WriteLine($"🖊️ PEN DOWN! Pressure: {pressure:F3}");
                PenDown?.Invoke(this, new PenEventArgs(pressure));
            }
            else if (inContact && isInContact && Math.Abs(pressure - lastPressure) > 0.01f)
            {
                // Pen move with pressure change
                lastPressure = pressure;
                Console.WriteLine($"✏️ PEN MOVE! Pressure: {pressure:F3}");
                PenMove?.Invoke(this, new PenEventArgs(pressure));
            }
            else if (!inContact && isInContact)
            {
                // Pen up
                isInContact = false;
                lastPressure = 0.0f;
                Console.WriteLine("🖊️ PEN UP!");
                PenUp?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    Stop();
                }

                isDisposed = true;
            }

            base.Dispose(disposing);
        }

        #region Native Interop

        /// <summary>
        /// Pointer input types.
        /// </summary>
        private enum PointerInputType : uint
        {
            PT_POINTER = 0x00000001,
            PT_TOUCH = 0x00000002,
            PT_PEN = 0x00000003,
            PT_MOUSE = 0x00000004,
            PT_TOUCHPAD = 0x00000005
        }

        /// <summary>
        /// Pointer flags.
        /// </summary>
        [Flags]
        #pragma warning disable S2346 // Enum member names should not be duplicated
        private enum PointerFlags : uint
        {
            NONE = 0x00000000,
            NEW = 0x00000001,
            INRANGE = 0x00000002,
            INCONTACT = 0x00000004,
            FIRSTBUTTON = 0x00000010,
            SECONDBUTTON = 0x00000020,
            THIRDBUTTON = 0x00000040,
            FOURTHBUTTON = 0x00000080,
            FIFTHBUTTON = 0x00000100,
            PRIMARY = 0x00002000,
            CONFIDENCE = 0x000004000,
            CANCELED = 0x000008000,
            DOWN = 0x00010000,
            UPDATE = 0x00020000,
            UP = 0x00040000,
            WHEEL = 0x00080000,
            HWHEEL = 0x00100000,
            CAPTURECHANGED = 0x00200000,
            HASTRANSFORM = 0x00400000
        }
        #pragma warning restore S2346

        /// <summary>
        /// Pointer information structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        #pragma warning disable SA1307 // Field names should begin with upper-case letter (native structure fields)
        private struct PointerInfo
        {
            public PointerInputType pointerType;
            public uint pointerId;
            public uint frameId;
            public PointerFlags pointerFlags;
            public IntPtr sourceDevice;
            public IntPtr hwndTarget;
            public System.Drawing.Point ptPixelLocation;
            public System.Drawing.Point ptHimetricLocation;
            public System.Drawing.Point ptPixelLocationRaw;
            public System.Drawing.Point ptHimetricLocationRaw;
            public uint dwTime;
            public uint historyCount;
            public int inputData;
            public uint dwKeyStates;
            public ulong PerformanceCount;
            public uint ButtonChangeType;
        }
        #pragma warning restore SA1307

        /// <summary>
        /// Pen-specific pointer information.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        #pragma warning disable SA1307 // Field names should begin with upper-case letter (native structure fields)
        private struct PointerPenInfo
        {
            public PointerInfo pointerInfo;
            public uint penFlags;
            public uint penMask;
            public uint pressure;
            public uint rotation;
            public int tiltX;
            public int tiltY;
        }
        #pragma warning restore SA1307

        [DllImport("user32.dll")]
        private static extern bool GetPointerInfo(uint pointerId, out PointerInfo pointerInfo);

        [DllImport("user32.dll")]
        private static extern bool GetPointerPenInfo(uint pointerId, out PointerPenInfo penInfo);

        #endregion
    }
}
