using AKAvR_IS.Classes.PythonExecution;
using AKAvR_IS.Classes.RequestParams;
using AKAvR_IS.Interfaces.IPythonExecutor;

namespace AKAvR_IS.Services
{
    public class PythonExecutorService : IPythonExecutorService, IDisposable
    {
        private readonly SemaphoreSlim        _semaphore;
        private readonly PythonExecutorConfig _config = new PythonExecutorConfig();
        
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
        }

        public PythonExecutorService(int maxConcurrentExecutions)
        {
            if (maxConcurrentExecutions <= 0)
                throw new ArgumentException("Max concurrent executions must be greater than 0", nameof(maxConcurrentExecutions));

            _semaphore = new SemaphoreSlim(maxConcurrentExecutions, maxConcurrentExecutions);
        }

        public async Task<PythonExecutionResult> ExecuteScriptAsync(string scriptPath, CancellationToken cancellationToken = default)
        {
            return await ExecuteScriptAsync(scriptPath, Array.Empty<IRequestParams>(), cancellationToken);
        }

        public async Task<PythonExecutionResult> ExecuteScriptAsync(string scriptPath, IEnumerable<IRequestParams> parameters, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(scriptPath))
                throw new ArgumentException("Script path cannot be null or empty", nameof(scriptPath));

            if (!File.Exists(scriptPath))
                throw new FileNotFoundException($"Python script not found: {scriptPath}");

            await _semaphore.WaitAsync(cancellationToken);
            Interlocked.Increment(ref _activeExecutions);
            Interlocked.Increment(ref _totalExecutions);

            var executor = new PythonExecutor();
            ConfigureExecutor(executor);

            try
            {
                var startTime = DateTime.Now;

                // Добавляем путь к скрипту как первый параметр
                var allParams = new List<IRequestParams>
                {
                    new ScriptPathRequestParams { ScriptPath = scriptPath }
                };

                if (parameters != null)
                {
                    allParams.AddRange(parameters);
                }

                executor.SetRequestParams(allParams.ToArray());

                await executor.ExecuteAsync(cancellationToken);

                var executionTime = DateTime.Now - startTime;
                Interlocked.Exchange(ref _lastExecutionDateTicks, DateTime.Now.Ticks);
                Interlocked.Add(ref _totalExecutionTicks, executionTime.Ticks);

                var result = new PythonExecutionResult
                {
                    Success        = !executor.HasErrors,
                    ScriptPath     = scriptPath,
                    Output         = executor.GetLastExecutionOutput(),
                    Error          = executor.GetLastError(),
                    ExitCode       = executor.GetLastExitCode(),
                    ExecutionTime  = executionTime,
                    ResponseParams = executor.GetResponseParams()
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
                    ExecutionTime  = TimeSpan.Zero,
                    ResponseParams = new IResponseParams[]
                    {
                        new ErrorResponseParams { ErrorMessage = ex.Message }
                    }
                };
            }
            finally
            {
                _semaphore.Release();
                Interlocked.Decrement(ref _activeExecutions);
            }
        }

        public PythonExecutionResult ExecuteScript(string scriptPath)
        {
            return ExecuteScript(scriptPath, Array.Empty<IRequestParams>());
        }

        public PythonExecutionResult ExecuteScript(string scriptPath, IEnumerable<IRequestParams> parameters)
        {
            return ExecuteScriptAsync(scriptPath, parameters).GetAwaiter().GetResult();
        }

        public async Task<IEnumerable<PythonExecutionResult>> ExecuteScriptsAsync(IEnumerable<string> scriptPaths, CancellationToken cancellationToken = default)
        {
            if (scriptPaths == null)
                throw new ArgumentNullException(nameof(scriptPaths));

            var tasks = new List<Task<PythonExecutionResult>>();

            foreach (var scriptPath in scriptPaths)
            {
                tasks.Add(ExecuteScriptAsync(scriptPath, cancellationToken));
            }

            return await Task.WhenAll(tasks);
        }

        public void Configure(Action<PythonExecutorConfig> configureAction)
        {
            if (configureAction == null)
                throw new ArgumentNullException(nameof(configureAction));

            configureAction(_config);
        }

        public PythonExecutorConfig GetCurrentConfig()
        {
            return new PythonExecutorConfig
            {
                PythonPath             = _config.PythonPath,
                WorkingDirectory       = _config.WorkingDirectory,
                TimeoutSeconds         = _config.TimeoutSeconds,
                RedirectStandardOutput = _config.RedirectStandardOutput,
                RedirectStandardError  = _config.RedirectStandardError,
                OutputEncoding         = _config.OutputEncoding
            };
        }

        public PythonExecutionStatistics GetStatistics()
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
                var executor = new PythonExecutor();
                ConfigureExecutor(executor);
                return executor.ValidateScript();
            }
            catch
            {
                return false;
            }
        }

        public bool ValidateScript(string scriptPath)
        {
            return !string.IsNullOrWhiteSpace(scriptPath) &&
                   File.Exists(scriptPath) &&
                   Path.GetExtension(scriptPath).ToLower() == ".py";
        }

        private void ConfigureExecutor(PythonExecutor executor)
        {
            executor.SetPythonPath(_config.PythonPath);
            executor.SetWorkingDirectory(_config.WorkingDirectory);
            executor.SetTimeout(_config.Timeout);
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}
