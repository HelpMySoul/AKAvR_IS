using AKAvR_IS.Classes.PythonExecution;
using AKAvR_IS.Classes.Structures.PythonExecutor;
using AKAvR_IS.Interfaces.IPythonExecutor;
using System.Diagnostics;

namespace AKAvR_IS.Services
{
    public class PythonExecutorService : IPythonExecutorService, IDisposable
    {
        private readonly SemaphoreSlim         _semaphore;
        private readonly IPythonExecutorConfig pythonExecutorConfig;

        private int _activeExecutions     = 0;
        private int _totalExecutions      = 0;
        private int _successfulExecutions = 0;

        private long _totalExecutionTicks    = 0;
        private long _lastExecutionDateTicks = DateTime.MinValue.Ticks; 

        public bool IsBusy          => _activeExecutions > 0;
        public int ActiveExecutions => _activeExecutions;

        public PythonExecutorService()
        {
            _semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
            pythonExecutorConfig = new PythonExecutorConfig();
        }

        public PythonExecutorService(int maxConcurrentExecutions)
        {
            if (maxConcurrentExecutions <= 0)
                throw new ArgumentException("Max concurrent executions must be greater than 0", nameof(maxConcurrentExecutions));

            _semaphore = new SemaphoreSlim(maxConcurrentExecutions, maxConcurrentExecutions);
            pythonExecutorConfig = new PythonExecutorConfig();
        }

        public async Task<IPythonExecutionResult> ExecuteScriptAsync(string scriptName, CancellationToken cancellationToken = default)
        {         
            return await ExecuteScriptAsync(scriptName, Array.Empty<IRequestParam>(), cancellationToken);
        }

        public async Task<IPythonExecutionResult> ExecuteScriptAsync(string scriptName, IEnumerable<IRequestParam> parameters, CancellationToken cancellationToken = default)
        {
            string scriptPath = GetScriptPath(scriptName);

            if (!ValidateScriptDirectory(scriptName))
                return new PythonExecutionResult
                {
                    Success = false,
                    ScriptPath = scriptPath,
                    Error = $"Service execution failed: script directory does not exist",
                    ExitCode = -1,
                    ExecutionTime = TimeSpan.Zero                    
                };            

            await _semaphore.WaitAsync(cancellationToken);
            Interlocked.Increment(ref _activeExecutions);
            Interlocked.Increment(ref _totalExecutions);

            PythonExecutor pythonExecutor = new PythonExecutor();
            pythonExecutor.SetFileName(scriptName);
            pythonExecutor.SetRequestParams(parameters.ToArray());
            pythonExecutor.SetWorkingDirectory(pythonExecutorConfig.WorkingDirectory);

            try
            {
                var startTime = DateTime.Now;
                
                await pythonExecutor.ExecuteAsync(cancellationToken);

                var executionTime = DateTime.Now - startTime;
                Interlocked.Exchange(ref _lastExecutionDateTicks, DateTime.Now.Ticks);
                Interlocked.Add(ref _totalExecutionTicks, executionTime.Ticks);

                var result = new PythonExecutionResult
                {
                    Success        = !pythonExecutor.HasErrors,
                    ScriptPath     = scriptPath,
                    Output         = pythonExecutor.GetLastExecutionOutput(),
                    Error          = pythonExecutor.GetLastError(),
                    ExitCode       = pythonExecutor.GetLastExitCode(),
                    ExecutionTime  = executionTime
                };

                if (result.Success)
                {
                    Interlocked.Increment(ref _successfulExecutions);
                }

                return result;
            }
            catch (Exception ex)
            {
                return new PythonExecutionResult
                {
                    Success        = false,
                    ScriptPath     = scriptPath,
                    Error          = $"Service execution failed: {ex.Message}",
                    ExitCode       = -1,
                    ExecutionTime  = TimeSpan.Zero
                };
            }
            finally
            {
                _semaphore.Release();
                Interlocked.Decrement(ref _activeExecutions);
            }
        }

        public IPythonExecutionResult ExecuteScript(string scriptName)
        {
            string scriptPath = GetScriptPath(scriptName);
            return ExecuteScript(scriptPath, Array.Empty<IRequestParam>());
        }

        public IPythonExecutionResult ExecuteScript(string scriptName, IEnumerable<IRequestParam> parameters)
        {
            string scriptPath = GetScriptPath(scriptName);
            return ExecuteScriptAsync(scriptPath, parameters).GetAwaiter().GetResult();
        }

        public async Task<IEnumerable<IPythonExecutionResult>> ExecuteScriptsAsync(IEnumerable<ScriptData> scriptDatas, CancellationToken cancellationToken = default)
        {
            if (scriptDatas == null)
                throw new ArgumentNullException(nameof(scriptDatas));

            var tasks = new List<Task<IPythonExecutionResult>>();

            foreach (var scriptData in scriptDatas)
            {
                tasks.Add(ExecuteScriptAsync(scriptData.ScriptName, scriptData.Parameters, cancellationToken));
            }

            return await Task.WhenAll(tasks);
        }

        public void Configure(Action<IPythonExecutorConfig> configureAction)
        {
            if (configureAction == null)
                throw new ArgumentNullException(nameof(configureAction));

            configureAction(pythonExecutorConfig);
        }

        public IPythonExecutorConfig GetCurrentConfig()
        {            
            return pythonExecutorConfig;
        }

        public IPythonExecutionStatistics GetStatistics()
        {
            var total              = _totalExecutions;
            var successful         = _successfulExecutions;
            var totalExecutionTime = TimeSpan.FromTicks(Interlocked.Read(ref _totalExecutionTicks));

            return new PythonExecutionStatistics
            {
                TotalExecutions      = total,
                SuccessfulExecutions = successful,
                FailedExecutions     = total - successful,
                TotalExecutionTime   = totalExecutionTime,
                AverageExecutionTime = total > 0 ? TimeSpan.FromTicks(totalExecutionTime.Ticks / total) : TimeSpan.Zero,
                LastExecutionDate    = new DateTime(Interlocked.Read(ref _lastExecutionDateTicks))
            };
        }

        public async Task<ILibraryInstallationResult> InstallLibrariesAsync(IEnumerable<string> libraries, CancellationToken cancellationToken = default)
        {
            return await InstallLibrariesAsync(libraries, null, "", cancellationToken);
        }

        public async Task<ILibraryInstallationResult> InstallLibrariesAsync(IEnumerable<string> libraries, Dictionary<string, string>? versions, string extraPipOptions, CancellationToken cancellationToken = default)
        {
            if (libraries == null || !libraries.Any())
            {
                return new LibraryInstallationResult
                {
                    Success = false,
                    Message = "No libraries provided for installation",
                    InstalledLibraries = new List<string>(),
                    FailedLibraries = new List<string>(),
                    InstallationTime = DateTime.Now
                };
            }

            // Проверяем доступность pip
            if (!IsPipAvailable())
            {
                return new LibraryInstallationResult
                {
                    Success = false,
                    Message = "Pip is not available on the system. Please ensure Python and pip are installed.",
                    InstalledLibraries = new List<string>(),
                    FailedLibraries = libraries.ToList(),
                    InstallationTime = DateTime.Now
                };
            }

            await _semaphore.WaitAsync(cancellationToken);
            Interlocked.Increment(ref _activeExecutions);

            var installed = new List<string>();
            var failed = new List<string>();

            try
            {
                foreach (var library in libraries)
                {
                    try
                    {
                        Console.WriteLine($"Starting installation of {library}...");

                        string version = versions != null && versions.ContainsKey(library)
                            ? $"=={versions[library]}"
                            : "";

                        var result = await InstallLibraryAsync(library, version, extraPipOptions, cancellationToken);

                        if (result)
                        {
                            Console.WriteLine($"Successfully installed {library}");
                            installed.Add(library);
                        }
                        else
                        {
                            Console.WriteLine($"Failed to install {library}");
                            failed.Add(library);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception during installation of {library}: {ex.Message}");
                        failed.Add(library);
                    }
                }

                var message = failed.Any()
                    ? $"Installed {installed.Count} libraries, failed to install {failed.Count} libraries"
                    : $"Successfully installed all {installed.Count} libraries";

                Console.WriteLine($"Installation completed: {message}");

                return new LibraryInstallationResult
                {
                    Success = !failed.Any(),
                    Message = message,
                    InstalledLibraries = installed,
                    FailedLibraries = failed,
                    InstallationTime = DateTime.Now
                };
            }
            finally
            {
                _semaphore.Release();
                Interlocked.Decrement(ref _activeExecutions);
            }
        }

        private async Task<bool> InstallLibraryAsync(string libraryName, string version, string extraPipOptions, CancellationToken cancellationToken)
        {
            try
            {
                using (var process = new Process())
                {
                    var packageSpec = string.IsNullOrEmpty(version)
                        ? libraryName
                        : $"{libraryName}{version}";

                    var arguments = $"install {packageSpec}";

                    // Добавим флаг --user для установки в домашнюю директорию пользователя
                    // Это может помочь с правами доступа
                    arguments += " --quiet --user";

                    if (!string.IsNullOrEmpty(extraPipOptions))
                    {
                        arguments += $" {extraPipOptions}";
                    }

                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = GetPipExecutable(),
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = pythonExecutorConfig.WorkingDirectory,
                        // Добавим переменные окружения, если нужно
                        Environment = {
                    ["PYTHONPATH"] = pythonExecutorConfig.WorkingDirectory
                }
                    };

                    Console.WriteLine($"Executing: {process.StartInfo.FileName} {process.StartInfo.Arguments}");

                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    // Логируем вывод для отладки
                    if (!string.IsNullOrEmpty(output))
                        Console.WriteLine($"Pip output: {output}");
                    if (!string.IsNullOrEmpty(error))
                        Console.WriteLine($"Pip error: {error}");

                    await process.WaitForExitAsync(cancellationToken);

                    Console.WriteLine($"Pip exit code: {process.ExitCode} for library: {libraryName}");

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception installing {libraryName}: {ex.Message}");
                return false;
            }
        }

        private string GetPipExecutable()
        {
            // Пробуем разные варианты имени pip
            var possiblePipNames = new[] { "pip", "pip3", "python3 -m pip", "python -m pip", "py -m pip" };

            foreach (var pipName in possiblePipNames)
            {
                try
                {
                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = pipName.Contains(" ") ? pipName.Split(' ')[0] : pipName,
                            Arguments = pipName.Contains(" ") ? string.Join(" ", pipName.Split(' ').Skip(1)) + " --version" : "--version",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        process.Start();
                        process.WaitForExit(2000);

                        if (process.ExitCode == 0)
                        {
                            Console.WriteLine($"Using pip executable: {pipName}");
                            return pipName;
                        }
                    }
                }
                catch
                {
                    // Продолжаем пробовать следующий вариант
                }
            }

            Console.WriteLine("No pip executable found, using default 'pip'");
            return "pip";
        }
        public bool IsPipAvailable()
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = GetPipExecutable().Contains(" ")
                            ? GetPipExecutable().Split(' ')[0]
                            : GetPipExecutable(),
                        Arguments = GetPipExecutable().Contains(" ")
                            ? string.Join(" ", GetPipExecutable().Split(' ').Skip(1)) + " --version"
                            : "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    process.Start();
                    process.WaitForExit(2000);

                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool ValidatePythonEnvironment()
        {
            try
            {                
                if (string.IsNullOrEmpty(pythonExecutorConfig.FileName))
                    return false;

                if (!File.Exists(pythonExecutorConfig.FileName) && !IsCommandAvailable(pythonExecutorConfig.FileName))
                    return false;

                if (!Directory.Exists(pythonExecutorConfig.WorkingDirectory))
                    return false;

                return true;       
                
            }
            catch
            {
                return false;
            }
        }

        private bool IsCommandAvailable(string fileName)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "python3",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    process.Start();
                    process.WaitForExit(1000);
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool ValidateScriptDirectory(string scriptName)
        {
            string scriptPath = GetScriptPath(scriptName);
            return !string.IsNullOrWhiteSpace(scriptPath) &&
                   File.Exists(scriptPath) &&
                   Path.GetExtension(scriptPath).ToLower() == ".py";
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }

        private string GetScriptPath(string scriptName)
        {
            return pythonExecutorConfig.WorkingDirectory + "/" + scriptName;
        }
    }
}
