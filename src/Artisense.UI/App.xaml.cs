// Copyright (c) Artisense. All rights reserved.

using System;
using System.Runtime.InteropServices;
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
        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeConsole();

        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="e">Startup event arguments.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Allocate console for debugging
            AllocConsole();
            Console.WriteLine("🚀 Artisense starting up...");
            Console.WriteLine("📋 Console output is now visible!");

            try
            {
                // Build and start the host
                host = CreateHost();
                host.Start();
                
                Console.WriteLine("✅ Host started successfully");

                // Initialize tray manager
                var serviceProvider = host.Services;
                trayManager = new TrayManager(
                    serviceProvider.GetRequiredService<ILogger<TrayManager>>(),
                    serviceProvider.GetRequiredService<IArtisenseController>());

                trayManager.Initialize();
                
                Console.WriteLine("✅ Tray manager initialized");
                Console.WriteLine("🖊️ Ready for pen input - try drawing!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Startup failed: {ex.Message}");
                Console.WriteLine($"📄 Stack trace: {ex.StackTrace}");
                
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
                Console.WriteLine("🛑 Shutting down Artisense...");
                trayManager?.Dispose();
                host?.StopAsync().Wait(TimeSpan.FromSeconds(5));
                host?.Dispose();
                Console.WriteLine("✅ Shutdown complete");
            }
            catch (Exception ex)
            {
                // Log but don't prevent shutdown
                Console.WriteLine($"❌ Error during shutdown: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
            }
            finally
            {
                FreeConsole();
            }

            base.OnExit(e);
        }

        private static IHost CreateHost()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register logging with debug level
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Debug);
                        builder.AddConsole();
                        builder.AddDebug();
                    });

                    // Register input services
                    services.AddSingleton<GlobalHookPenProvider>();
                    services.AddSingleton<WindowsInkPenProvider>();
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
