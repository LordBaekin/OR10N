using CoffeeFlow.ViewModel;
using System;

public static class Logger
{
    public static void LogStatus(string message)
    {
        // Adjust this to send logs to your desired output (e.g., Debug window, file, etc.)
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} {message}");

        // If you want to integrate with MainViewModel's DebugList:
        MainViewModel.Instance?.DebugList?.Add($"{DateTime.Now:HH:mm:ss} {message}");
    }
}
