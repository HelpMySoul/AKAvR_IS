using AKAvR_IS.Classes.Execution;
using AKAvR_IS.Classes.Execution.Batch;
using AKAvR_IS.Classes.RequestParams;
using AKAvR_IS.Classes.Structures.PythonExecutor;
using AKAvR_IS.Interfaces.IPythonExecutor;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("api/[controller]")]
public class PythonExecutorController : ControllerBase
{
    private readonly IPythonExecutorService _pythonExecutorService;

    public PythonExecutorController(IPythonExecutorService pythonExecutorService)
    {
        _pythonExecutorService = pythonExecutorService;
    }

    [HttpPost("execute")]
    public async Task<ActionResult<ExecutionResult>> ExecuteScript([FromBody] ExecuteScriptRequest request)
    {
        try
        {
            // Подготовка параметров
            var parameters = new List<IRequestParam>();
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

            
            // Выполнение через сервис
            var result = await _pythonExecutorService.ExecuteScriptAsync(request.ScriptName, parameters);

            if (result.Success)
            {
                return Ok(new ExecutionResult
                {
                    Success            = result.Success,
                    Output             = result.Output,
                    Error              = result.Error,
                    ExitCode           = result.ExitCode,
                    ExecutionTime      = result.ExecutionTime,
                    ExecutionDate      = result.ExecutionDate
                });
            }
            else
            {
                return StatusCode(500, new ExecutionResult
                {
                    Success            = result.Success,
                    Output             = result.Output,
                    Error              = result.Error,
                    ExitCode           = result.ExitCode,
                    ExecutionTime      = result.ExecutionTime,
                    ExecutionDate      = result.ExecutionDate
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ExecutionResult
            {
                Success = false,
                Error = $"Execution failed: {ex.Message}",
                ExitCode = -1,
                ExecutionDate = DateTime.Now
            });
        }
    }

    [HttpGet("validate")]
    public ActionResult<ExecutionValidationResult> ValidateEnvironment()
    {
        try
        {
            var isValid = _pythonExecutorService.ValidatePythonEnvironment();

            return Ok(new ExecutionValidationResult
            {
                IsValid = isValid,
                Message = isValid ? "Python environment is properly configured" : "Python environment is not properly configured",
                ValidationTime = DateTime.Now
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
        var stats = _pythonExecutorService.GetStatistics();

        return Ok(new ExecutionStatus
        {
            IsExecuting          = _pythonExecutorService.IsBusy,
            ActiveExecutions     = _pythonExecutorService.ActiveExecutions,
            TotalExecutions      = stats.TotalExecutions,
            SuccessfulExecutions = stats.SuccessfulExecutions,
            FailedExecutions     = stats.FailedExecutions,
            LastExecutionTime    = stats.LastExecutionDate,
            AverageExecutionTime = stats.AverageExecutionTime
        });
    }

    [HttpPost("batch-execute")]
    public async Task<ActionResult<BatchExecutionResult>> BatchExecute([FromBody] BatchExecuteRequest requests)
    {
        List<ScriptData> scriptDatas = new List<ScriptData>();

        foreach (var request in requests.ScriptRequests)
        {
            var parameters = new List<IRequestParam>();
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
            Results = results.Select(r => new ExecutionResult
            {
                Success            = r.Success,
                Output             = r.Output,
                Error              = r.Error,
                ExitCode           = r.ExitCode,
                ExecutionTime      = r.ExecutionTime,
                ExecutionDate      = r.ExecutionDate
            }).ToList()
        });
    }
}