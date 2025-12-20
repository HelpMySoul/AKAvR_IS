using AKAVER_Server.Interfaces.IPythonExecutor;

namespace AKAVER_Server.Classes.Execution
{
    public class ExecutionResult
    {
        public bool Success           { get; set; }
        public string? Output         { get; set; }
        public string? Error          { get; set; }
        public int ExitCode           { get; set; }
        public string? PythonPath     { get; set; }
        public string? PipCommand     { get; set; }
        public TimeSpan ExecutionTime { get; set; }        
        public DateTime ExecutionDate { get; set; }

    }
}
