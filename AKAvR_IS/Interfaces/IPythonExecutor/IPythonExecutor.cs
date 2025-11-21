namespace AKAvR_IS.Interfaces.IPythonExecutor
{
    public interface IPythonExecutor
    {
        // Основные методы выполнения
        public void SetRequestParams(params IRequestParam[] requestParams);
        public void Execute();
        public IResponseParam[] GetResponseParams();
        public Task ExecuteAsync(CancellationToken cancellationToken = default);
        public bool ValidateScript();

        // Конфигурация
        public void SetFileName(string fileName);
        public void SetWorkingDirectory(string workingDirectory);
        public void SetTimeout(TimeSpan timeout);

        public IPythonExecutorConfig GetConfig();

        // Логирование и диагностика
        public string GetLastExecutionOutput();
        public string GetLastError();
        public int GetLastExitCode();

        // Состояние
        public bool IsExecuting { get; }
        public bool HasErrors { get; }
        public DateTime LastExecutionTime { get; }
    }

    public interface IRequestParam
    {
        string ParameterName { get; }
        object Value { get; }
        Type ValueType { get; }
    }

    public interface IResponseParam
    {
        string ParameterName { get; }
        object Value { get; }
        Type ValueType { get; }
        bool IsSuccess { get; }
        string ErrorMessage { get; }
    }              
}