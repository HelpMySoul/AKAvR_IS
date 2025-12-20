using AKAVER_Server.Interfaces.IPythonExecutor;

namespace AKAVER_Server.Classes.PythonExecution
{
    public class PythonExecutionResult : IPythonExecutionResult
    {
        public bool Success             { get; set; }
        public string ScriptPath        { get; set; } = string.Empty;
        public string Output            { get; set; } = string.Empty;
        public string Error             { get; set; } = string.Empty;
        public int ExitCode             { get; set; }
        public TimeSpan ExecutionTime   { get; set; }
        public DateTime ExecutionDate   { get; set; } = DateTime.Now;
    }
}
