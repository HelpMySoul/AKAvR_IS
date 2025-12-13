using AKAvR_IS.Interfaces.Execute;

namespace AKAvR_IS.Classes.Execution
{
    public class ExecutionStatus : IExecutionStatus
    {
        public bool IsExecuting                 { get; set; }
        public bool HasErrors                   { get; set; }
        public DateTime LastExecutionTime       { get; set; }
        public string? LastOutput               { get; set; }
        public string? LastError                { get; set; }
        public int LastExitCode                 { get; set; }
        public int ActiveExecutions             { get; set; }
        public int TotalExecutions              { get; set; }
        public int SuccessfulExecutions         { get; set; }
        public int FailedExecutions             { get; set; }
        public TimeSpan AverageExecutionTime    { get; set; }
        public string? PythonPath               { get; set; }
        public string? PipCommand               { get; set; }
        public DateTime CheckTime               { get; set; }
        public string? Message                  { get; set; }
    }
}
