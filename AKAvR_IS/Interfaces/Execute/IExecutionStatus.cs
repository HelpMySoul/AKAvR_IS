namespace AKAvR_IS.Interfaces.Execute
{
    public interface IExecutionStatus
    {
        bool IsExecuting                 { get; set; }
        bool HasErrors                   { get; set; }
        DateTime LastExecutionTime       { get; set; }
        string? LastOutput               { get; set; }
        string? LastError                { get; set; }
        int LastExitCode                 { get; set; }
        int ActiveExecutions             { get; set; }
        int TotalExecutions              { get; set; }
        int SuccessfulExecutions         { get; set; }
        int FailedExecutions             { get; set; }
        TimeSpan AverageExecutionTime    { get; set; }
        string? PythonPath               { get; set; }
        string? PipCommand               { get; set; }
        DateTime CheckTime               { get; set; }
        string? Message                  { get; set; }
    }
}