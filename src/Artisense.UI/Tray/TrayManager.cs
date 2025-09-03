// Copyright (c) Artisense. All rights reserved.

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Artisense.Core.CoreController;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.Logging;

namespace Artisense.UI.Tray
{
    /// <summary>
    /// Manages the system tray icon and menu for Artisense.
    /// </summary>
    public class TrayManager : IDisposable
    {
        private readonly ILogger<TrayManager> logger;
        private readonly IArtisenseController controller;
        private TaskbarIcon? trayIcon;
        private ContextMenu? contextMenu;
        private MenuItem? enabledMenuItem;
#pragma warning disable S1450 // Field used for UI element lifecycle management
        private Slider? volumeSlider;
#pragma warning restore S1450
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrayManager"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="controller">The Artisense controller.</param>
        public TrayManager(ILogger<TrayManager> logger, IArtisenseController controller)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        /// <summary>
        /// Initializes the tray icon and menu.
        /// </summary>
        public void Initialize()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(TrayManager));
            }

            try
            {
                // Create tray icon
                trayIcon = new TaskbarIcon
                {
                    Icon = Properties.Resources.ArtisenseIcon,
                    ToolTipText = "Artisense - Pressure-sensitive drawing sounds"
                };

                // Create context menu
                CreateContextMenu();
                trayIcon.ContextMenu = contextMenu;

                // Enable by default
                controller.Enable();
                UpdateEnabledMenuItemText();

                logger.LogInformation("Tray manager initialized successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize tray manager");
                throw;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the tray manager and optionally releases the managed resources.
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
                try
                {
                    controller?.Disable();
                    trayIcon?.Dispose();
                    trayIcon = null;
                    contextMenu = null;
                    enabledMenuItem = null;
                    volumeSlider = null;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error disposing tray manager");
                }
            }

            isDisposed = true;
        }

        private void CreateContextMenu()
        {
            contextMenu = new ContextMenu();

            // Sound enabled toggle
            enabledMenuItem = new MenuItem
            {
                Header = "✔ Sound Enabled",
                IsCheckable = true,
                IsChecked = true
            };
            enabledMenuItem.Click += OnEnabledMenuItemClick;
            contextMenu.Items.Add(enabledMenuItem);

            contextMenu.Items.Add(new Separator());

            // Volume offset section
            var volumeHeader = new MenuItem
            {
                Header = "Volume Offset",
                IsEnabled = false
            };
            contextMenu.Items.Add(volumeHeader);

            // Volume slider container
            var volumeContainer = new MenuItem();
            volumeSlider = new Slider
            {
                Minimum = -12.0,
                Maximum = 0.0,
                Value = 0.0,
                Width = 120,
                Margin = new Thickness(10, 5, 10, 5),
                TickFrequency = 2.0,
                IsSnapToTickEnabled = true,
                TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight
            };
            volumeSlider.ValueChanged += OnVolumeSliderValueChanged;
            volumeContainer.Header = volumeSlider;
            contextMenu.Items.Add(volumeContainer);

            // Volume value display
            var volumeLabel = new MenuItem
            {
                Header = "0 dB",
                IsEnabled = false
            };
            contextMenu.Items.Add(volumeLabel);

            // Update volume label when slider changes
            volumeSlider.ValueChanged += (s, e) =>
            {
                volumeLabel.Header = $"{e.NewValue:F0} dB";
            };

            contextMenu.Items.Add(new Separator());

            // Exit menu item
            var exitMenuItem = new MenuItem
            {
                Header = "Exit"
            };
            exitMenuItem.Click += OnExitMenuItemClick;
            contextMenu.Items.Add(exitMenuItem);
        }

        private void OnEnabledMenuItemClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (enabledMenuItem?.IsChecked == true)
                {
                    controller.Enable();
                    logger.LogInformation("Artisense enabled via tray menu");
                }
                else
                {
                    controller.Disable();
                    logger.LogInformation("Artisense disabled via tray menu");
                }

                UpdateEnabledMenuItemText();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error toggling Artisense state");
            }
        }

        private void OnVolumeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                controller.VolumeOffsetDb = (float)e.NewValue;
                logger.LogDebug("Volume offset changed to {VolumeDb:F1} dB", e.NewValue);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error changing volume offset");
            }
        }

        private void OnExitMenuItemClick(object sender, RoutedEventArgs e)
        {
            try
            {
                logger.LogInformation("Exit requested via tray menu");
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during application shutdown");
                Environment.Exit(1);
            }
        }

        private void UpdateEnabledMenuItemText()
        {
            if (enabledMenuItem != null)
            {
                enabledMenuItem.Header = controller.IsEnabled ? "✔ Sound Enabled" : "✘ Sound Disabled";
            }
        }
    }
}
