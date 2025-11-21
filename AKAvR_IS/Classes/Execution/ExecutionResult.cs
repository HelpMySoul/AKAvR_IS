using AKAvR_IS.Interfaces.IPythonExecutor;

namespace AKAvR_IS.Classes.Execution
{
    public class ExecutionResult
    {
        public bool Success { get; set; }
        public string? Output { get; set; }
        public string? Error { get; set; }
        public int ExitCode { get; set; }
        public TimeSpan ExecutionTime { get; set; }        
        public DateTime ExecutionDate { get; set; }

    }
}
