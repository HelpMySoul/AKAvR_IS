using Microsoft.Extensions.Configuration;
using System.Diagnostics;

internal class PythonEnvironmentHelper : IPythonEnvironmentHelper
{
    private readonly IConfiguration _configuration;
    private readonly string _pythonPath;
    private readonly string _pythonVersion;

    public PythonEnvironmentHelper(IConfiguration configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _configuration = configuration;

        _pythonPath = _configuration["Python:ExecutablePath"]!;

        if (string.IsNullOrEmpty(_pythonPath))
        {
            throw new InvalidOperationException(
                "Python:ExecutablePath is not configured in appsettings.json");
        }

        _pythonVersion = _configuration["Python:Version"] ?? "3.11";

        ValidatePythonPath();
    }

    public string GetPipExecutable()
    {
        return $"{_pythonPath} -m pip";
    }

    public string GetPythonExecutable()
    {
        return _pythonPath;
    }

    public string GetPyVersion()
    {
        return _pythonVersion;
    }

    private void ValidatePythonPath()
    {
        if (string.IsNullOrEmpty(_pythonPath))
        {
            throw new InvalidOperationException(
                "Python executable path is not configured. " +
                "Please set 'Python:ExecutablePath' in appsettings.json");
        }

        if (!File.Exists(_pythonPath))
        {
            throw new FileNotFoundException(
                $"Python executable not found at: {_pythonPath}");
        }

        // Проверяем, что это действительно Python
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(3000);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"File at {_pythonPath} is not a valid Python executable");
            }

            Console.WriteLine($"Using configured Python: {_pythonPath}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to validate Python at {_pythonPath}: {ex.Message}");
        }
    }
}