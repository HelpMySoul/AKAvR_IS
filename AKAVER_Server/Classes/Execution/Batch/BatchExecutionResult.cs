namespace AKAVER_Server.Classes.Execution.Batch
{
    public class BatchExecutionResult
    {
        public int TotalExecutions           { get; set; }
        public int SuccessfulExecutions      { get; set; }
        public int FailedExecutions          { get; set; }
        public DateTime ExecutionTime        { get; set; }
        public string? PythonPath            { get; set; }
        public string? Message               { get; set; }
        public List<ExecutionResult> Results { get; set; } = new List<ExecutionResult>();
    }
}
