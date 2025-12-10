using System.Diagnostics;

internal class PythonEnvironmentHelper : IPythonEnvironmentHelper
{
    private string _cachePythonExecutable = "";
    private readonly object _lock = new object();
    private bool _isSearchPerformed = false;

    public PythonEnvironmentHelper()
    {

    }

    public string GetPipExecutable()
    {
        var pythonExe = GetPythonExecutable();
        return $"{pythonExe} -m pip";
    }

    public string GetPythonExecutable()
    {
        // Если уже выполнялся поиск и есть кэшированный результат
        if (_isSearchPerformed && !string.IsNullOrEmpty(_cachePythonExecutable))
        {
            return _cachePythonExecutable;
        }

        lock (_lock)
        {
            // Двойная проверка для потокобезопасности
            if (_isSearchPerformed && !string.IsNullOrEmpty(_cachePythonExecutable))
            {
                return _cachePythonExecutable;
            }

            var result = FindPythonExecutable();
            _cachePythonExecutable = result;
            _isSearchPerformed = true;

            return result;
        }
    }

    public void ClearCache()
    {
        lock (_lock)
        {
            _cachePythonExecutable = "";
            _isSearchPerformed = false;
        }
    }

    private string FindPythonExecutable()
    {
        var possiblePythonNames = new List<string>
        {
            "python3",
            "python",
            "python.exe",
            "py",
            "python3.exe"
        };

        foreach (var pythonName in possiblePythonNames)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = pythonName,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    process.Start();
                    process.WaitForExit(2000);

                    if (process.ExitCode == 0)
                    {
                        var version = process.StandardOutput.ReadToEnd();
                        if (string.IsNullOrEmpty(version))
                            version = process.StandardError.ReadToEnd();

                        Console.WriteLine($"Found Python at: {pythonName} ({version.Trim()})");
                        return pythonName;
                    }
                }
            }
            catch
            {
                
            }
        }

        Console.WriteLine("Python not found. Using 'python3' as default.");
        return "python3";
    }
}