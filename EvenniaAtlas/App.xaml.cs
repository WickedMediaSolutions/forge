using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace EvenniaAtlas;

public partial class App : Application
{
    public App()
    {
        // TEMPORARY startup exception logging
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteCrashLog(e.Exception);
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            WriteCrashLog(ex);
    }

    private static void WriteCrashLog(Exception ex)
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "forge-crash.log");
            File.WriteAllText(path, ex.ToString());
        }
        catch
        {
            // deliberately swallow — logging must never cause another crash
        }
    }
}