using AKAVER_Server.Interfaces;
using AKAVER_Server.Interfaces.IPythonExecutor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AKAVER_Server.Classes.PythonExecution
{
    public class PythonExecutor : IPythonExecutor
    {
        private readonly List<IRequestParam> _requestParams  = new List<IRequestParam>();
        private IResponseParam[] _responseParams             = Array.Empty<IResponseParam>();
        private PythonExecutorConfig _config                 = new PythonExecutorConfig();
        private string _lastOutput                           = string.Empty;
        private string _lastError                            = string.Empty;
        private int _lastExitCode                            = 0;
        private DateTime _lastExecutionTime                  = DateTime.MinValue;
        private bool _isExecuting                            = false;

        public bool IsExecuting           => _isExecuting;
        public bool HasErrors             => !string.IsNullOrEmpty(_lastError) || _lastExitCode != 0;
        public DateTime LastExecutionTime => _lastExecutionTime;

        public void SetRequestParams(params IRequestParam[] requestParams)
        {
            _requestParams.Clear();
            if (requestParams != null)
            {
                _requestParams.AddRange(requestParams);
            }
        }

        public void Execute()
        {
            ExecuteAsync().GetAwaiter().GetResult();
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (_isExecuting)
                throw new InvalidOperationException("Execution is already in progress");

            _isExecuting  = true;
            _lastOutput   = string.Empty;
            _lastError    = string.Empty;
            _lastExitCode = 0;

            try
            {
                var startTime = DateTime.Now;

                Console.WriteLine("=== Python launch params ===");
                Console.WriteLine($"FileName:               {_config.FileName}");
                Console.WriteLine($"Arguments:              {BuildPythonArguments()}");
                Console.WriteLine($"PythonPath:             {_config.PythonPath}");
                Console.WriteLine($"WorkingDirectory:       {_config.WorkingDirectory}");
                Console.WriteLine($"CsvInputFolder:         {_config.CsvInputFolder}");
                Console.WriteLine($"CsvOutputFolder:        {_config.CsvOutputFolder}");
                Console.WriteLine($"RedirectStandardOutput: {_config.RedirectStandardOutput}");
                Console.WriteLine($"RedirectStandardError:  {_config.RedirectStandardError}");
                Console.WriteLine($"UseShellExecute:        {false}");
                Console.WriteLine($"CreateNoWindow:         {true}");
                Console.WriteLine($"StandardOutputEncoding: {_config.OutputEncoding?.EncodingName ?? "null"}");
                Console.WriteLine($"StandardErrorEncoding:  {_config.OutputEncoding?.EncodingName ?? "null"}");
                Console.WriteLine("=================================");

                
                using (var process = new Process())
                {

                    var pythonScript = _config.FileName;
                    var arguments = BuildPythonArguments(_config.CsvInputFolder);
                    
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName                = _config.PythonPath,
                        Arguments               = $"{pythonScript} {arguments}",
                        WorkingDirectory        = _config.WorkingDirectory,
                        RedirectStandardOutput  = _config.RedirectStandardOutput,
                        RedirectStandardError   = _config.RedirectStandardError,
                        UseShellExecute         = false,
                        CreateNoWindow          = true,
                        StandardOutputEncoding  = _config.OutputEncoding,
                        StandardErrorEncoding   = _config.OutputEncoding
                    };

                    var outputBuilder = new StringBuilder();
                    var errorBuilder  = new StringBuilder();

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            outputBuilder.AppendLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            errorBuilder.AppendLine(e.Data);
                        }
                    };

                    process.Start();

                    if (_config.RedirectStandardOutput)
                        process.BeginOutputReadLine();

                    if (_config.RedirectStandardError)
                        process.BeginErrorReadLine();

                    var processTask = Task.Run(() =>
                    {
                        process.WaitForExit();
                        return process.ExitCode;
                    }, cancellationToken);

                    var timeoutTask   = Task.Delay(_config.Timeout, cancellationToken);
                    var completedTask = await Task.WhenAny(processTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch (InvalidOperationException) { }

                        throw new TimeoutException($"Python execution timed out after {_config.Timeout}");
                    }

                    _lastExitCode      = await processTask;
                    _lastOutput        = outputBuilder.ToString();
                    _lastError         = errorBuilder.ToString();
                    _lastExecutionTime = DateTime.Now;

                    ProcessResponse(_lastOutput, _lastError, _lastExitCode);
                }
            }
            catch (Exception ex)
            {
                _lastError = $"Execution failed: {ex.Message}";
                _responseParams = new IResponseParam[]
                {
                    new ErrorResponseParams { ErrorMessage = _lastError }
                };
                throw new PythonExecutionException("Python execution failed", ex);
            }
            finally
            {
                _isExecuting = false;
            }
        }

        public IResponseParam[] GetResponseParams()
        {
            return _responseParams;
        }

        public bool ValidateScript()
        {
            if (string.IsNullOrEmpty(_config.FileName))
                return false;

            if (!File.Exists(_config.FileName) && !IsCommandAvailable(_config.FileName))
                return false;

            if (!Directory.Exists(_config.WorkingDirectory))
                return false;

            return true;
        }

        public void SetFileName(string fileName)
        {
            _config.FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        }

        public void SetWorkingDirectory(string workingDirectory)
        {
            _config.WorkingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
        }

        public void SetPythonPath(string pythonPath)
        {
            _config.PythonPath = pythonPath ?? throw new ArgumentNullException(nameof(pythonPath));
        }

        public void SetTimeout(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentException("Timeout must be positive", nameof(timeout));

            _config.TimeoutSeconds = timeout.Seconds;
        }

        public string GetLastExecutionOutput()
        {
            return _lastOutput;
        }

        public string GetLastError()
        {
            return _lastError;
        }

        public int GetLastExitCode()
        {
            return _lastExitCode;
        }

        private string BuildPythonArguments(string folder = "") 
        {            
            var args = new List<string>();

            foreach (var param in _requestParams)
            {
                args.Add(folder + Convert.ToString(param.Value) ?? string.Empty);
            }

            return string.Join(" ", args);
        }

        private void ProcessResponse(string output, string error, int exitCode)
        {
            var responses = new List<IResponseParam>();

            if (exitCode == 0 && string.IsNullOrEmpty(error))
            {
                try
                {
                    var successResponse = new SuccessResponseParams
                    {
                        Output = output,
                        ExecutionTime = _lastExecutionTime
                    };
                    responses.Add(successResponse);
                }
                catch (Exception ex)
                {
                    responses.Add(new ErrorResponseParams
                    {
                        ErrorMessage = $"Failed to parse output: {ex.Message}"
                    });
                }
            }
            else
            {
                responses.Add(new ErrorResponseParams
                {
                    ErrorMessage = error,
                    ExitCode = exitCode
                });
            }

            _responseParams = responses.ToArray();
        }

        private bool IsCommandAvailable(string command)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
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

        public IPythonExecutorConfig GetConfig()
        {
            return _config;
        }
    }

    public class CommandLineRequestParams : IRequestParam
    {
        public required string ParameterName { get; set; }
        public required Type ValueType { get; set; }

        public object Value { get; set; } = string.Empty;

        public string ToCommandLineArgument()
        {
            return $"{ParameterName} {Value}";
        }
    }

    public class SuccessResponseParams : IResponseParam
    {
        public string Output { get; set; } = string.Empty;
        public DateTime ExecutionTime { get; set; }

        public string ParameterName => "SuccessResult";
        public object Value => new { Output, ExecutionTime };
        public Type ValueType => typeof(object);
        public bool IsSuccess => true;
        public string ErrorMessage => string.Empty;
    }

    public class ErrorResponseParams : IResponseParam
    {
        public string ErrorMessage { get; set; } = string.Empty;
        public int ExitCode { get; set; }

        public string ParameterName => "ErrorResult";
        public object Value => new { ErrorMessage, ExitCode };
        public Type ValueType => typeof(object);
        public bool IsSuccess => false;
    }
}