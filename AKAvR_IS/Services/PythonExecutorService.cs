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
