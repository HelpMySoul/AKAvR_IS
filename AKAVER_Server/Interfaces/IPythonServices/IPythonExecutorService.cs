using AKAVER_Server.Classes.PythonExecution;
using AKAVER_Server.Classes.Structures.PythonExecutor;
using AKAVER_Server.Interfaces.Execute;
using AKAVER_Server.Interfaces.IPythonExecutor;
using AKAVER_Server.Interfaces.IPythonExecutorResult;

public interface IPythonExecutorService
{
    // Основные методы выполнения
    Task<IPythonExecutionResult> ExecuteScriptAsync(string scriptName, CancellationToken cancellationToken = default);
    Task<IPythonExecutionResult> ExecuteScriptAsync(string scriptName, IEnumerable<IRequestParam> parameters, CancellationToken cancellationToken = default);

    // Синхронные версии
    IPythonExecutionResult ExecuteScript(string scriptName);
    IPythonExecutionResult ExecuteScript(string scriptName, IEnumerable<IRequestParam> parameters);

    // Пакетное выполнение
    Task<IEnumerable<IPythonExecutionResult>> ExecuteScriptsAsync(IEnumerable<ScriptData> scriptDatas, CancellationToken cancellationToken = default);

    // Управление конфигурацией
    void Configure(Action<IPythonExecutorConfig> configureAction);
    IPythonExecutorConfig GetCurrentConfig();

    // Состояние и статистика
    bool IsBusy { get; }
    int ActiveExecutions { get; }
    IPythonExecutionStatistics GetStatistics();

    // Валидация
    bool ValidatePythonEnvironment();
    bool ValidateScriptDirectory(string scriptName);

    // Библиотеки
    Task<ILibraryInstallationResult> InstallLibrariesAsync(IEnumerable<string> libraries, CancellationToken cancellationToken = default);
    Task<ILibraryInstallationResult> InstallLibrariesAsync(IEnumerable<string> libraries, Dictionary<string, string> versions, string extraPipOptions, CancellationToken cancellationToken = default);

    bool IsPipAvailable();

    Task<ITestInstallResult> TestInstallationAsync(string library = "tensorflow>=2.10.0", CancellationToken cancellationToken = default);
    Task<ICheckInstalledResult> CheckInstalledPackagesAsync(CancellationToken cancellationToken = default);
    Task<IPythonVersionResult> CheckPythonVersionAsync(CancellationToken cancellationToken = default);
    IPipStatusResult GetPipStatus();
    IExecutionValidationResult ValidateEnvironment();
    IExecutionStatus GetStatus();
}