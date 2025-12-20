using AKAVER_Server.Classes.Execution;
using AKAVER_Server.Classes.Execution.Batch;
using AKAVER_Server.Classes.RequestParams;
using AKAVER_Server.Classes.Structures.PythonExecutor;
using AKAVER_Server.Interfaces.IPythonExecutor;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO.Pipelines;
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
        var result = await _pythonExecutorService.TestInstallationAsync();
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return StatusCode(500, result);
        }
    }

    [HttpPost("check-installed")]
    public async Task<ActionResult> CheckInstalled()
    {
        var result = await _pythonExecutorService.CheckInstalledPackagesAsync();
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return StatusCode(500, result);
        }
    }

    [HttpPost("check-python-version")]
    public async Task<ActionResult> CheckPythonVersion()
    {
        var result = await _pythonExecutorService.CheckPythonVersionAsync();
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return StatusCode(500, result);
        }
    }

    [HttpGet("pip-status")]
    public ActionResult<PipStatusResult> GetPipStatus()
    {
        var result = _pythonExecutorService.GetPipStatus();
        if (result.IsPipAvailable)
        {
            return Ok(result);
        }
        else
        {
            return StatusCode(500, result);
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
            Console.WriteLine($"Install libraries exception: {ex}");
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
                ExecutionDate = result.ExecutionTime == TimeSpan.Zero ? DateTime.Now : DateTime.Now - result.ExecutionTime,
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
        var result = _pythonExecutorService.ValidateEnvironment();
        if (result.IsValid)
        {
            return Ok(result);
        }
        else
        {
            return StatusCode(500, result);
        }
    }

    [HttpGet("status")]
    public ActionResult<ExecutionStatus> GetStatus()
    {
        var result = _pythonExecutorService.GetStatus();
        return Ok(result);
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