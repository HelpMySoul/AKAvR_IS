using AKAvR_IS.Classes.Execution;
using AKAvR_IS.Classes.PythonExecution;
using AKAvR_IS.Classes.PythonExecutorResult;
using AKAvR_IS.Classes.Structures.PythonExecutor;
using AKAvR_IS.Interfaces.Execute;
using AKAvR_IS.Interfaces.IPythonExecutor;
using AKAvR_IS.Interfaces.IPythonExecutorResult;
using System.Diagnostics;

namespace AKAvR_IS.Services
{
    public class PythonExecutorService : IPythonExecutorService, IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly IPythonExecutorConfig    _pythonExecutorConfig;
        private readonly IPythonEnvironmentHelper _pythonEnvironmentHelper;

        private int _activeExecutions     = 0;
        private int _totalExecutions      = 0;
        private int _successfulExecutions = 0;

        private long _totalExecutionTicks    = 0;
        private long _lastExecutionDateTicks = DateTime.MinValue.Ticks;

        public bool IsBusy => _activeExecutions > 0;
        public int ActiveExecutions => _activeExecutions;

        public PythonExecutorService(IPythonEnvironmentHelper pythonEnvironmentHelper)
        {
            _semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
            _pythonExecutorConfig    = new PythonExecutorConfig();
            _pythonEnvironmentHelper = pythonEnvironmentHelper ?? throw new ArgumentNullException(nameof(pythonEnvironmentHelper));
        }

        public PythonExecutorService(int maxConcurrentExecutions, IPythonEnvironmentHelper pythonEnvironmentHelper)
        {
            if (maxConcurrentExecutions <= 0)
                throw new ArgumentException("Max concurrent executions must be greater than 0", nameof(maxConcurrentExecutions));

            _semaphore = new SemaphoreSlim(maxConcurrentExecutions, maxConcurrentExecutions);
            _pythonExecutorConfig = new PythonExecutorConfig();
            _pythonEnvironmentHelper = pythonEnvironmentHelper ?? throw new ArgumentNullException(nameof(pythonEnvironmentHelper));
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
                    Success         = false,
                    ScriptPath      = scriptPath,
                    Error           = $"Service execution failed: script directory does not exist",
                    ExitCode        = -1,
                    ExecutionTime   = TimeSpan.Zero
                };

            await _semaphore.WaitAsync(cancellationToken);
            Interlocked.Increment(ref _activeExecutions);
            Interlocked.Increment(ref _totalExecutions);

            PythonExecutor pythonExecutor = new PythonExecutor();
            pythonExecutor.SetFileName(scriptName);
            pythonExecutor.SetRequestParams(parameters.ToArray());
            pythonExecutor.SetWorkingDirectory(_pythonExecutorConfig.WorkingDirectory);
            var pipCommand = _pythonEnvironmentHelper.GetPipExecutable();
            pythonExecutor.SetPythonPath(pipCommand.Contains(" ") ? pipCommand.Split(' ')[0] : pipCommand);

            try
            {
                var startTime = DateTime.Now;

                await pythonExecutor.ExecuteAsync(cancellationToken);

                var executionTime = DateTime.Now - startTime;
                Interlocked.Exchange(ref _lastExecutionDateTicks, DateTime.Now.Ticks);
                Interlocked.Add(ref _totalExecutionTicks, executionTime.Ticks);

                var result = new PythonExecutionResult
                {
                    Success         = !pythonExecutor.HasErrors,
                    ScriptPath      = scriptPath,
                    Output          = pythonExecutor.GetLastExecutionOutput(),
                    Error           = pythonExecutor.GetLastError(),
                    ExitCode        = pythonExecutor.GetLastExitCode(),
                    ExecutionTime   = executionTime
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
                    Success         = false,
                    ScriptPath      = scriptPath,
                    Error           = $"Service execution failed: {ex.Message}",
                    ExitCode        = -1,
                    ExecutionTime   = TimeSpan.Zero
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

            configureAction(_pythonExecutorConfig);
        }

        public IPythonExecutorConfig GetCurrentConfig()
        {
            return _pythonExecutorConfig;
        }

        public IPythonExecutionStatistics GetStatistics()
        {
            var total = _totalExecutions;
            var successful = _successfulExecutions;
            var totalExecutionTime = TimeSpan.FromTicks(Interlocked.Read(ref _totalExecutionTicks));

            return new PythonExecutionStatistics
            {
                TotalExecutions         = total,
                SuccessfulExecutions    = successful,
                FailedExecutions        = total - successful,
                TotalExecutionTime      = totalExecutionTime,
                AverageExecutionTime    = total > 0 ? TimeSpan.FromTicks(totalExecutionTime.Ticks / total) : TimeSpan.Zero,
                LastExecutionDate       = new DateTime(Interlocked.Read(ref _lastExecutionDateTicks))
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
                    Success             = false,
                    Message             = "No libraries provided for installation",
                    InstalledLibraries  = new List<string>(),
                    FailedLibraries     = new List<string>(),
                    InstallationTime    = DateTime.Now
                };
            }

            // Проверяем доступность pip
            if (!IsPipAvailable())
            {
                return new LibraryInstallationResult
                {
                    Success             = false,
                    Message             = "Pip is not available on the system. Please ensure Python and pip are installed.",
                    InstalledLibraries  = new List<string>(),
                    FailedLibraries     = libraries.ToList(),
                    InstallationTime    = DateTime.Now
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
                    Success             = !failed.Any(),
                    Message             = message,
                    InstalledLibraries  = installed,
                    FailedLibraries     = failed,
                    InstallationTime    = DateTime.Now
                };
            }
            finally
            {
                _semaphore.Release();
                Interlocked.Decrement(ref _activeExecutions);
            }
        }

        public async Task<ITestInstallResult> TestInstallationAsync(string library = "tensorflow>=2.10.0", CancellationToken cancellationToken = default)
        {
            try
            {
                var pythonExe = _pythonEnvironmentHelper.GetPythonExecutable();
                var pipCommand = _pythonEnvironmentHelper.GetPipExecutable();

                Console.WriteLine($"Starting test installation of {library}...");
                Console.WriteLine($"Using Python: {pythonExe}");
                Console.WriteLine($"Using Pip command: {pipCommand}");

                using (var process = new Process())
                {
                    var fileName = pipCommand.Contains(" ") ? pipCommand.Split(' ')[0] : pipCommand;
                    var arguments = pipCommand.Contains(" ")
                        ? string.Join(" ", pipCommand.Split(' ').Skip(1)) + $" install --user --quiet --disable-pip-version-check {library}"
                        : $"install --user --quiet --disable-pip-version-check {library}";

                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName                = fileName,
                        Arguments               = arguments,
                        RedirectStandardOutput  = true,
                        RedirectStandardError   = true,
                        UseShellExecute         = false,
                        CreateNoWindow          = true
                    };

                    Console.WriteLine($"Executing: {fileName} {arguments}");

                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error  = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync(cancellationToken);

                    Console.WriteLine($"Test install exit code: {process.ExitCode}");
                    Console.WriteLine($"Output: {output}");
                    Console.WriteLine($"Error: {error}");

                    return new TestInstallResult
                    {
                        Success     = process.ExitCode == 0,
                        Message     = process.ExitCode == 0 ? "Test installation successful" : "Test installation failed",
                        PythonPath  = pythonExe,
                        PipCommand  = pipCommand,
                        ExitCode    = process.ExitCode,
                        Output      = output,
                        Error       = error
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test install exception: {ex}");
                return new TestInstallResult
                {
                    Success       = false,
                    Message       = $"Test installation error: {ex.Message}",
                    ExceptionType = ex.GetType().Name,
                    StackTrace    = ex.StackTrace
                };
            }
        }

        public async Task<ICheckInstalledResult> CheckInstalledPackagesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var pythonExe  = _pythonEnvironmentHelper.GetPythonExecutable();
                var pipCommand = _pythonEnvironmentHelper.GetPipExecutable();

                Console.WriteLine($"Checking installed packages...");
                Console.WriteLine($"Using Python: {pythonExe}");
                Console.WriteLine($"Using Pip command: {pipCommand}");

                using (var process = new Process())
                {
                    var fileName = pipCommand.Contains(" ") ? pipCommand.Split(' ')[0] : pipCommand;
                    var arguments = pipCommand.Contains(" ")
                        ? string.Join(" ", pipCommand.Split(' ').Skip(1)) + " list --format=json"
                        : "list --format=json";

                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName                = fileName,
                        Arguments               = arguments,
                        RedirectStandardOutput  = true,
                        RedirectStandardError   = true,
                        UseShellExecute         = false,
                        CreateNoWindow          = true
                    };

                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error  = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync(cancellationToken);

                    return new CheckInstalledResult
                    {
                        Success     = process.ExitCode == 0,
                        PythonPath  = pythonExe,
                        Output      = output,
                        Error       = error,
                        ExitCode    = process.ExitCode
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Check installed packages exception: {ex}");
                return new CheckInstalledResult
                {
                    Success = false,
                    Message = $"Error checking installed packages: {ex.Message}"
                };
            }
        }

        public async Task<IPythonVersionResult> CheckPythonVersionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var pythonExe = _pythonEnvironmentHelper.GetPythonExecutable();

                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName                = pythonExe,
                        Arguments               = "--version",
                        RedirectStandardOutput  = true,
                        RedirectStandardError   = true,
                        UseShellExecute         = false,
                        CreateNoWindow          = true
                    };

                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync(cancellationToken);

                    return new PythonVersionResult
                    {
                        Success         = process.ExitCode == 0,
                        PythonPath      = pythonExe,
                        VersionOutput   = output,
                        Error           = error,
                        ExitCode        = process.ExitCode
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Check Python version exception: {ex}");
                return new PythonVersionResult
                {
                    Success = false,
                    Message = $"Error checking Python version: {ex.Message}"
                };
            }
        }

        public IPipStatusResult GetPipStatus()
        {
            try
            {
                var isAvailable = IsPipAvailable();
                var pythonExe = _pythonEnvironmentHelper.GetPythonExecutable();
                var pipCommand = _pythonEnvironmentHelper.GetPipExecutable();

                return new PipStatusResult
                {
                    IsPipAvailable  = isAvailable,
                    Message         = isAvailable ? "Pip is available on the system" : "Pip is not available",
                    CheckTime       = DateTime.Now,
                    PythonPath      = pythonExe,
                    PipCommand      = pipCommand
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get pip status exception: {ex}");
                return new PipStatusResult
                {
                    IsPipAvailable = false,
                    Message        = $"Error checking pip status: {ex.Message}",
                    CheckTime      = DateTime.Now
                };
            }
        }

        public IExecutionValidationResult ValidateEnvironment()
        {
            try
            {
                var isValid     = ValidatePythonEnvironment();
                var pythonExe   = _pythonEnvironmentHelper.GetPythonExecutable();
                var pipCommand  = _pythonEnvironmentHelper.GetPipExecutable();

                return new ExecutionValidationResult
                {
                    IsValid         = isValid,
                    Message         = isValid ? "Python environment is properly configured" : "Python environment is not properly configured",
                    ValidationTime  = DateTime.Now,
                    PythonPath      = pythonExe,
                    PipCommand      = pipCommand
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Validate environment exception: {ex}");
                return new ExecutionValidationResult
                {
                    IsValid        = false,
                    Message        = $"Validation failed: {ex.Message}",
                    ValidationTime = DateTime.Now
                };
            }
        }

        public IExecutionStatus GetStatus()
        {
            try
            {
                var stats       = GetStatistics();
                var pythonExe   = _pythonEnvironmentHelper.GetPythonExecutable();
                var pipCommand  = _pythonEnvironmentHelper.GetPipExecutable();

                return new ExecutionStatus
                {
                    IsExecuting          = IsBusy,
                    ActiveExecutions     = ActiveExecutions,
                    TotalExecutions      = stats.TotalExecutions,
                    SuccessfulExecutions = stats.SuccessfulExecutions,
                    FailedExecutions     = stats.FailedExecutions,
                    LastExecutionTime    = stats.LastExecutionDate,
                    AverageExecutionTime = stats.AverageExecutionTime,
                    PythonPath           = pythonExe,
                    PipCommand           = pipCommand,
                    CheckTime            = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get status exception: {ex}");
                return new ExecutionStatus
                {
                    IsExecuting = false,
                    Message     = $"Error getting status: {ex.Message}",
                    CheckTime   = DateTime.Now
                };
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

                    arguments += " --quiet --user";

                    if (!string.IsNullOrEmpty(extraPipOptions))
                    {
                        arguments += $" {extraPipOptions}";
                    }

                    var pipCommand = _pythonEnvironmentHelper.GetPipExecutable();
                    var fileName = pipCommand.Contains(" ") ? pipCommand.Split(' ')[0] : pipCommand;
                    var baseArgs = pipCommand.Contains(" ")
                        ? string.Join(" ", pipCommand.Split(' ').Skip(1))
                        : "";

                    if (!string.IsNullOrEmpty(baseArgs))
                    {
                        arguments = $"{baseArgs} {arguments}";
                    }

                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName                = fileName,
                        Arguments               = arguments,
                        RedirectStandardOutput  = true,
                        RedirectStandardError   = true,
                        UseShellExecute         = false,
                        CreateNoWindow          = true,
                        WorkingDirectory        = _pythonExecutorConfig.WorkingDirectory,
                        Environment = {
                            ["PYTHONPATH"] = _pythonExecutorConfig.WorkingDirectory
                        }
                    };

                    Console.WriteLine($"Executing: {process.StartInfo.FileName} {process.StartInfo.Arguments}");

                    process.Start();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error  = await process.StandardError.ReadToEndAsync();

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
            return _pythonEnvironmentHelper.GetPipExecutable();
        }

        public bool IsPipAvailable()
        {
            try
            {
                using (var process = new Process())
                {
                    var pipCommand = _pythonEnvironmentHelper.GetPipExecutable();
                    var fileName = pipCommand.Contains(" ")
                        ? pipCommand.Split(' ')[0]
                        : pipCommand;
                    var arguments = pipCommand.Contains(" ")
                        ? string.Join(" ", pipCommand.Split(' ').Skip(1)) + " --version"
                        : "--version";

                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
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
                if (string.IsNullOrEmpty(_pythonExecutorConfig.FileName))
                    return false;

                if (!File.Exists(_pythonExecutorConfig.FileName) && !IsCommandAvailable(_pythonExecutorConfig.FileName))
                    return false;

                if (!Directory.Exists(_pythonExecutorConfig.WorkingDirectory))
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
            return Path.Combine(_pythonExecutorConfig.WorkingDirectory, scriptName);
        }
    }
}