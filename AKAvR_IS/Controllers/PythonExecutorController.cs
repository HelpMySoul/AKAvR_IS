using AKAvR_IS.Classes.Execution;
using AKAvR_IS.Classes.Execution.Batch;
using AKAvR_IS.Classes.RequestParams;
using AKAvR_IS.Classes.Structures.PythonExecutor;
using AKAvR_IS.Interfaces.IPythonExecutor;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
public class PythonExecutorController : ControllerBase
{
    private readonly IPythonExecutorService   _pythonExecutorService;
    private readonly IPythonEnvironmentHelper _pythonEnvironmentHelper;

    public PythonExecutorController(IPythonExecutorService pythonExecutorService, IPythonEnvironmentHelper pythonEnvironmentHelper)
    {
        _pythonExecutorService   = pythonExecutorService;
        _pythonEnvironmentHelper = pythonEnvironmentHelper;
    }

    [HttpPost("test-install")]
    public async Task<ActionResult> TestInstall()
    {
        try
        {
            Console.WriteLine("Starting test installation...");

            var pythonExe  = _pythonEnvironmentHelper.GetPythonExecutable();
            var pipCommand = _pythonEnvironmentHelper.GetPipExecutable();

            Console.WriteLine($"Using Python: {pythonExe}");
            Console.WriteLine($"Using Pip command: {pipCommand}");

            using (var process = new Process())
            {
                var fileName  = pipCommand.Contains(" ") ? pipCommand.Split(' ')[0] : pipCommand;
                var arguments = pipCommand.Contains(" ")
                    ? string.Join(" ", pipCommand.Split(' ').Skip(1)) + " install --user --quiet --disable-pip-version-check colorama"
                    : "install --user --quiet --disable-pip-version-check colorama";

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

                await process.WaitForExitAsync();

                Console.WriteLine($"Test install exit code: {process.ExitCode}");
                Console.WriteLine($"Output: {output}");
                Console.WriteLine($"Error: {error}");

                if (process.ExitCode == 0)
                {
                    return Ok(new
                    {
                        success    = true,
                        message    = "Test installation successful",
                        python     = pythonExe,
                        pipCommand = pipCommand,
                        exitCode   = process.ExitCode
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        success    = false,
                        message    = "Test installation failed",
                        python     = pythonExe,
                        pipCommand = pipCommand,
                        exitCode   = process.ExitCode,
                        error      = error
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test install exception: {ex}");
            return StatusCode(500, new
            {
                success       = false,
                message       = $"Test installation error: {ex.Message}",
                exceptionType = ex.GetType().Name,
                stackTrace    = ex.StackTrace
            });
        }
    }

    [HttpPost("check-installed")]
    public async Task<ActionResult> CheckInstalled([FromBody] CheckInstalledRequest request)
    {
        try
        {
            var pythonExe = _pythonEnvironmentHelper.GetPythonExecutable();

            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName               = pythonExe,
                    Arguments              = "-m pip list --format=json",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error  = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                return Ok(new
                {
                    success    = process.ExitCode == 0,
                    pythonPath = pythonExe,
                    output     = output,
                    error      = error,
                    exitCode   = process.ExitCode
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = $"Error checking installed packages: {ex.Message}"
            });
        }
    }

    [HttpGet("pip-status")]
    public ActionResult<PipStatusResult> GetPipStatus()
    {
        try
        {
            var isAvailable = _pythonExecutorService.IsPipAvailable();
            var pythonExe   = _pythonEnvironmentHelper.GetPythonExecutable();
            var pipCommand  = _pythonEnvironmentHelper.GetPipExecutable();

            return Ok(new PipStatusResult
            {
                IsPipAvailable = isAvailable,
                Message = isAvailable ? "Pip is available on the system" : "Pip is not available",
                CheckTime = DateTime.Now,
                PythonPath = pythonExe,
                PipCommand = pipCommand
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new PipStatusResult
            {
                IsPipAvailable = false,
                Message        = $"Error checking pip status: {ex.Message}",
                CheckTime      = DateTime.Now
            });
        }
    }

    [HttpPost("install-libs")]
    public async Task<ActionResult<LibraryInstallationResult>> InstallLibs([FromBody] InstallLibrariesRequest installRequest)
    {
        try
        {
            if (installRequest == null || installRequest.Libraries == null || !installRequest.Libraries.Any())
            {
                return BadRequest(new LibraryInstallationResult
                {
                    Success            = false,
                    Message            = "No libraries provided for installation",
                    InstalledLibraries = new List<string>(),
                    FailedLibraries    = new List<string>(),
                    InstallationTime   = DateTime.Now
                });
            }

            var pythonExe  = _pythonEnvironmentHelper.GetPythonExecutable();
            var pipCommand = _pythonEnvironmentHelper.GetPipExecutable();

            Console.WriteLine($"Installing libraries using Python: {pythonExe}");
            Console.WriteLine($"Pip command: {pipCommand}");
            Console.WriteLine($"Libraries to install: {string.Join(", ", installRequest.Libraries)}");

            var installationResult = await _pythonExecutorService.InstallLibrariesAsync(
                installRequest.Libraries,
                installRequest.LibraryVersions,
                installRequest.ExtraPipOptions);

            var result = new LibraryInstallationResult
            {
                Success            = installationResult.Success,
                Message            = installationResult.Message,
                InstalledLibraries = installationResult.InstalledLibraries,
                FailedLibraries    = installationResult.FailedLibraries,
                InstallationTime   = DateTime.Now,
                PythonPath         = pythonExe,
                PipCommand         = pipCommand
            };

            if (installationResult.Success)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new LibraryInstallationResult
            {
                Success            = false,
                Message            = $"Installation failed: {ex.Message}",
                InstalledLibraries = new List<string>(),
                FailedLibraries    = installRequest?.Libraries?.ToList() ?? new List<string>(),
                InstallationTime   = DateTime.Now,
                PythonPath         = _pythonEnvironmentHelper?.GetPythonExecutable() ?? "Unknown",
                PipCommand         = _pythonEnvironmentHelper?.GetPipExecutable() ?? "Unknown"
            });
        }
    }

    [HttpPost("execute")]
    public async Task<ActionResult<ExecutionResult>> ExecuteScript([FromBody] ExecuteScriptRequest request)
    {
        try
        {
            var pythonExe = _pythonEnvironmentHelper.GetPythonExecutable();

            Console.WriteLine($"Executing script '{request.ScriptName}' using Python: {pythonExe}");

            var parameters = new List<IRequestParam>();
            if (request.Parameters != null)
            {
                foreach (var param in request.Parameters)
                {
                    parameters.Add(new CustomRequestParams
                    {
                        ParameterName = param.Key,
                        Value         = param.Value
                    });
                }
            }

            var result = await _pythonExecutorService.ExecuteScriptAsync(request.ScriptName, parameters);

            var executionResult = new ExecutionResult
            {
                Success       = result.Success,
                Output        = result.Output,
                Error         = result.Error,
                ExitCode      = result.ExitCode,
                ExecutionTime = result.ExecutionTime,
                ExecutionDate = result.ExecutionDate,
                PythonPath    = pythonExe
            };

            if (result.Success)
            {
                return Ok(executionResult);
            }
            else
            {
                return StatusCode(500, executionResult);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ExecutionResult
            {
                Success       = false,
                Error         = $"Execution failed: {ex.Message}",
                ExitCode      = -1,
                ExecutionDate = DateTime.Now,
                PythonPath    = _pythonEnvironmentHelper?.GetPythonExecutable() ?? "Unknown"
            });
        }
    }

    [HttpGet("validate")]
    public ActionResult<ExecutionValidationResult> ValidateEnvironment()
    {
        try
        {
            var isValid    = _pythonExecutorService.ValidatePythonEnvironment();
            var pythonExe  = _pythonEnvironmentHelper.GetPythonExecutable();
            var pipCommand = _pythonEnvironmentHelper.GetPipExecutable();

            return Ok(new ExecutionValidationResult
            {
                IsValid        = isValid,
                Message        = isValid ? "Python environment is properly configured" : "Python environment is not properly configured",
                ValidationTime = DateTime.Now,
                PythonPath     = pythonExe,
                PipCommand     = pipCommand
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ExecutionValidationResult
            {
                IsValid = false,
                Message = $"Validation failed: {ex.Message}",
                ValidationTime = DateTime.Now
            });
        }
    }

    [HttpGet("status")]
    public ActionResult<ExecutionStatus> GetStatus()
    {
        try
        {
            var stats      = _pythonExecutorService.GetStatistics();
            var pythonExe  = _pythonEnvironmentHelper.GetPythonExecutable();
            var pipCommand = _pythonEnvironmentHelper.GetPipExecutable();

            return Ok(new ExecutionStatus
            {
                IsExecuting          = _pythonExecutorService.IsBusy,
                ActiveExecutions     = _pythonExecutorService.ActiveExecutions,
                TotalExecutions      = stats.TotalExecutions,
                SuccessfulExecutions = stats.SuccessfulExecutions,
                FailedExecutions     = stats.FailedExecutions,
                LastExecutionTime    = stats.LastExecutionDate,
                AverageExecutionTime = stats.AverageExecutionTime,
                PythonPath           = pythonExe,
                PipCommand           = pipCommand,
                CheckTime            = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ExecutionStatus
            {
                IsExecuting = false,
                Message     = $"Error getting status: {ex.Message}",
                CheckTime   = DateTime.Now
            });
        }
    }

    [HttpPost("batch-execute")]
    public async Task<ActionResult<BatchExecutionResult>> BatchExecute([FromBody] BatchExecuteRequest requests)
    {
        try
        {
            var pythonExe = _pythonEnvironmentHelper.GetPythonExecutable();

            Console.WriteLine($"Batch executing {requests.ScriptRequests?.Count ?? 0} scripts using Python: {pythonExe}");

            List<ScriptData> scriptDatas = new List<ScriptData>();
            foreach (var (request, parameters) in from request in requests.ScriptRequests
                                                  let parameters = new List<IRequestParam>()
                                                  select (request, parameters))
            {
                if (request.Parameters != null)
                {
                    foreach (var param in request.Parameters)
                    {
                        parameters.Add(new CustomRequestParams
                        {
                            ParameterName = param.Key,
                            Value = param.Value
                        });
                    }
                }

                scriptDatas.Add(new ScriptData(request.ScriptName, parameters));
            }

            var results = await _pythonExecutorService.ExecuteScriptsAsync(scriptDatas);

            return Ok(new BatchExecutionResult
            {
                TotalExecutions      = results.Count(),
                SuccessfulExecutions = results.Count(r => r.Success),
                FailedExecutions     = results.Count(r => !r.Success),
                PythonPath           = pythonExe,
                ExecutionTime        = DateTime.Now,
                Results              = results.Select(r => new ExecutionResult
                {
                    Success          = r.Success,
                    Output           = r.Output,
                    Error            = r.Error,
                    ExitCode         = r.ExitCode,
                    ExecutionTime    = r.ExecutionTime,
                    ExecutionDate    = r.ExecutionDate,
                    PythonPath       = pythonExe
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new BatchExecutionResult
            {
                TotalExecutions      = 0,
                SuccessfulExecutions = 0,
                FailedExecutions     = 0,
                Message              = $"Batch execution failed: {ex.Message}",
                ExecutionTime        = DateTime.Now,
                PythonPath           = _pythonEnvironmentHelper?.GetPythonExecutable() ?? "Unknown"
            });
        }
    }    
}