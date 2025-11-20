using AKAvR_IS.Classes.PythonExecution;
using AKAvR_IS.Interfaces.IPythonExecutor;

public interface IPythonExecutorService
{
    // Основные методы выполнения
    Task<PythonExecutionResult> ExecuteScriptAsync(string scriptPath, CancellationToken cancellationToken = default);
    Task<PythonExecutionResult> ExecuteScriptAsync(string scriptPath, IEnumerable<IRequestParams> parameters, CancellationToken cancellationToken = default);

    // Синхронные версии
    PythonExecutionResult ExecuteScript(string scriptPath);
    PythonExecutionResult ExecuteScript(string scriptPath, IEnumerable<IRequestParams> parameters);

    // Пакетное выполнение
    Task<IEnumerable<PythonExecutionResult>> ExecuteScriptsAsync(IEnumerable<string> scriptPaths, CancellationToken cancellationToken = default);

    // Управление конфигурацией
    void Configure(Action<PythonExecutorConfig> configureAction);
    PythonExecutorConfig GetCurrentConfig();

    // Состояние и статистика
    bool IsBusy { get; }
    int ActiveExecutions { get; }
    PythonExecutionStatistics GetStatistics();

    // Валидация
    bool ValidatePythonEnvironment();
    bool ValidateScript(string scriptPath);
}