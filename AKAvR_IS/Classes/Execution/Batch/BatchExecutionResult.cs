namespace AKAvR_IS.Classes.Execution.Batch
{
    public class BatchExecutionResult
    {
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public List<ExecutionResult> Results { get; set; } = new List<ExecutionResult>();
    }
}
