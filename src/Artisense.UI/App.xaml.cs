// Copyright (c) Artisense. All rights reserved.

using System;
using System.Windows;
using Artisense.Core.AudioService;
using Artisense.Core.CoreController;
using Artisense.Core.InputService;
using Artisense.UI.Tray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Artisense.UI
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        private IHost? host;
        private TrayManager? trayManager;

        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="e">Startup event arguments.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Build and start the host
                host = CreateHost();
                host.Start();

                // Initialize tray manager
                var serviceProvider = host.Services;
                trayManager = new TrayManager(
                    serviceProvider.GetRequiredService<ILogger<TrayManager>>(),
                    serviceProvider.GetRequiredService<IArtisenseController>());

                trayManager.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to start Artisense: {ex.Message}",
                    "Artisense Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(1);
            }
        }

        /// <summary>
        /// Application exit handler.
        /// </summary>
        /// <param name="e">Exit event arguments.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                trayManager?.Dispose();
                host?.StopAsync().Wait(TimeSpan.FromSeconds(5));
                host?.Dispose();
            }
            catch (Exception ex)
            {
                // Log but don't prevent shutdown
                System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
            }

            base.OnExit(e);
        }

        private static IHost CreateHost()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register logging
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Information);
                        builder.AddConsole();
                        builder.AddDebug();
                    });

                    // Register input services
                    services.AddSingleton<RawInputPenProvider>();
                    services.AddSingleton<WintabPenProvider>();
                    services.AddSingleton<PenInputService>();

                    // Register audio service
                    services.AddSingleton<IAudioService, WasapiAudioService>();

                    // Register core controller
                    services.AddSingleton<IArtisenseController, ArtisenseController>();

                    // Register hosted services
                    services.AddHostedService<PenInputService>(provider => 
                        provider.GetRequiredService<PenInputService>());
                })
                .UseConsoleLifetime()
                .Build();
        }
    }
}
