using System.Diagnostics;

namespace Hutch.Relay.Commands.Runners;

public class RunEfDatabase
{
  public void Run(string command)
  {
    Console.WriteLine($"Running EF command: dotnet ef {command}");
    try
    {
      var startInfo = new ProcessStartInfo
      {
        FileName = "dotnet",
        Arguments = $"ef database {command}",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      using var process = Process.Start(startInfo);
      if (process == null)
      {
        Console.WriteLine("Failed to start process.");
        return;
      }

      using var reader = process.StandardOutput;
      var output = reader.ReadToEnd();
      Console.WriteLine(output);

      using var errorReader = process.StandardError;
      var errors = errorReader.ReadToEnd();
      if (!string.IsNullOrEmpty(errors))
      {
        Console.WriteLine($"Error: {errors}");
      }

      process.WaitForExit();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error executing EF command: {ex.Message}");
    }
  }
}
