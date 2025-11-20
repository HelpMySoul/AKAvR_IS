namespace AKAvR_IS.Interfaces.IPythonExecutor
{
    public interface IPythonExecutor
    {
        // Основные методы выполнения
        public void SetRequestParams(params IRequestParams[] requestParams);
        public void Execute();
        public IResponseParams[] GetResponseParams();
        public Task ExecuteAsync(CancellationToken cancellationToken = default);
        public bool ValidateScript();

        // Конфигурация
        public void SetPythonPath(string pythonExecutablePath);
        public void SetWorkingDirectory(string workingDirectory);
        public void SetTimeout(TimeSpan timeout);

        // Логирование и диагностика
        public string GetLastExecutionOutput();
        public string GetLastError();
        public int GetLastExitCode();

        // Состояние
        public bool IsExecuting { get; }
        public bool HasErrors { get; }
        public DateTime LastExecutionTime { get; }
    }

    public interface IRequestParams
    {
        string ParameterName { get; }
        object Value { get; }
        Type ValueType { get; }
    }

    public interface IResponseParams
    {
        string ParameterName { get; }
        object Value { get; }
        Type ValueType { get; }
        bool IsSuccess { get; }
        string ErrorMessage { get; }
    }              
}