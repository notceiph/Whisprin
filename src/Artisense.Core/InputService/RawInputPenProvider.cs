// Copyright (c) Artisense. All rights reserved.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace Artisense.Core.InputService
{
    /// <summary>
    /// Pen input provider using Windows Raw Input API for global pen capture.
    /// </summary>
    public class RawInputPenProvider : IPenInputProvider
    {
        private readonly ILogger<RawInputPenProvider> logger;
        private readonly NativeWindow messageWindow;
        private bool isActive;
        private bool isDisposed;
        private bool isInContact;
        private float lastPressure;

        /// <summary>
        /// Initializes a new instance of the <see cref="RawInputPenProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public RawInputPenProvider(ILogger<RawInputPenProvider> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.messageWindow = new RawInputMessageWindow(this);
        }

        /// <inheritdoc/>
        public event EventHandler<PenEventArgs>? PenDown;

        /// <inheritdoc/>
        public event EventHandler<PenEventArgs>? PenMove;

        /// <inheritdoc/>
        public event EventHandler? PenUp;

        /// <inheritdoc/>
        public bool IsActive => isActive;

        /// <inheritdoc/>
        public bool Start()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(RawInputPenProvider));
            }

            if (isActive)
            {
                return true;
            }

            try
            {
                // Register for HID devices (stylus/pen devices)
                var rawInputDevice = new RawInputDevice
                {
                    UsagePage = HidUsagePage.GenericDesktop,
                    Usage = HidUsage.Pen,
                    Flags = RawInputDeviceFlags.InputSink,
                    Target = messageWindow.Handle
                };

                if (!RegisterRawInputDevices(new[] { rawInputDevice }, 1, (uint)Marshal.SizeOf<RawInputDevice>()))
                {
                    var error = Marshal.GetLastWin32Error();
                    logger.LogError("Failed to register raw input device. Error: {Error}", error);
                    return false;
                }

                isActive = true;
                logger.LogInformation("Raw Input pen provider started successfully");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start Raw Input pen provider");
                return false;
            }
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (!isActive)
            {
                return;
            }

            try
            {
                // Unregister the raw input device
                var rawInputDevice = new RawInputDevice
                {
                    UsagePage = HidUsagePage.GenericDesktop,
                    Usage = HidUsage.Pen,
                    Flags = RawInputDeviceFlags.Remove,
                    Target = IntPtr.Zero
                };

                RegisterRawInputDevices(new[] { rawInputDevice }, 1, (uint)Marshal.SizeOf<RawInputDevice>());
                isActive = false;
                logger.LogInformation("Raw Input pen provider stopped");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error stopping Raw Input pen provider");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the provider and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                Stop();
                messageWindow?.DestroyHandle();
            }

            isDisposed = true;
        }

        private void ProcessRawInput(IntPtr hRawInput)
        {
            try
            {
                // Get the size of the raw input data
                uint size = 0;
                if (GetRawInputData(hRawInput, RawInputDataCommand.Input, IntPtr.Zero, ref size, (uint)Marshal.SizeOf<RawInputHeader>()) != 0)
                {
                    return;
                }

                // Allocate buffer and get the raw input data
                var buffer = new byte[size];
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    if (GetRawInputData(hRawInput, RawInputDataCommand.Input, handle.AddrOfPinnedObject(), ref size, (uint)Marshal.SizeOf<RawInputHeader>()) != size)
                    {
                        return;
                    }

                    var rawInput = Marshal.PtrToStructure<RawInput>(handle.AddrOfPinnedObject());
                    
                    if (rawInput.Header.Type == RawInputType.Hid)
                    {
                        ProcessHidInput(rawInput.Hid);
                    }
                }
                finally
                {
                    handle.Free();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing raw input");
            }
        }

        private void ProcessHidInput(RawHid hidData)
        {
            // This is a simplified implementation - in practice, you'd need to parse
            // the HID report descriptor to understand the data format
            // For now, we'll simulate pen data extraction
            
            if (hidData.Size < 8)
            {
                return;
            }

            // Extract contact and pressure from HID data (device-specific parsing needed)
            // This is a placeholder implementation - real implementation would parse HID reports
            var contactByte = hidData.RawData[0];
            var pressureBytes = BitConverter.ToUInt16(hidData.RawData, 2);
            
            var inContact = (contactByte & 0x01) != 0;
            var pressure = Math.Clamp(pressureBytes / 1024.0f, 0.0f, 1.0f);

            // Process pen state changes
            if (inContact && !isInContact)
            {
                // Pen down
                isInContact = true;
                lastPressure = pressure;
                PenDown?.Invoke(this, new PenEventArgs(pressure));
            }
            else if (inContact && isInContact && Math.Abs(pressure - lastPressure) > 0.01f)
            {
                // Pen move with pressure change
                lastPressure = pressure;
                PenMove?.Invoke(this, new PenEventArgs(pressure));
            }
            else if (!inContact && isInContact)
            {
                // Pen up
                isInContact = false;
                lastPressure = 0.0f;
                PenUp?.Invoke(this, EventArgs.Empty);
            }
        }

        #region Native Interop

        private const int WM_INPUT = 0x00FF;

        [DllImport("user32.dll")]
        private static extern bool RegisterRawInputDevices(
            [MarshalAs(UnmanagedType.LPArray)] RawInputDevice[] rawInputDevices,
            uint numDevices,
            uint size);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(
            IntPtr hRawInput,
            RawInputDataCommand command,
            IntPtr data,
            ref uint size,
            uint sizeHeader);

        private enum RawInputDataCommand : uint
        {
            Input = 0x10000003,
            Header = 0x10000005
        }

        private enum RawInputType : uint
        {
            Mouse = 0,
            Keyboard = 1,
            Hid = 2
        }

        private enum RawInputDeviceFlags : uint
        {
            None = 0,
            Remove = 0x00000001,
            Exclude = 0x00000010,
            PageOnly = 0x00000020,
            NoLegacy = 0x00000030,
            InputSink = 0x00000100,
            CaptureMouse = 0x00000200,
            NoHotKeys = 0x00000200,
            AppKeys = 0x00000400,
            ExInputSink = 0x00001000,
            DevNotify = 0x00002000
        }

        private static class HidUsagePage
        {
            public const ushort GenericDesktop = 0x01;
        }

        private static class HidUsage
        {
            public const ushort Pen = 0x02;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RawInputDevice
        {
            public ushort UsagePage;
            public ushort Usage;
            public RawInputDeviceFlags Flags;
            public IntPtr Target;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RawInputHeader
        {
            public RawInputType Type;
            public uint Size;
            public IntPtr Device;
            public IntPtr WParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RawInput
        {
            public RawInputHeader Header;
            public RawHid Hid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RawHid
        {
            public uint Size;
            public uint Count;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] RawData;
        }

        #endregion

        /// <summary>
        /// Native window for receiving raw input messages.
        /// </summary>
        private class RawInputMessageWindow : NativeWindow
        {
            private readonly RawInputPenProvider provider;

            public RawInputMessageWindow(RawInputPenProvider provider)
            {
                this.provider = provider;
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_INPUT)
                {
                    provider.ProcessRawInput(m.LParam);
                }

                base.WndProc(ref m);
            }
        }
    }
}
