using AKAVER_Server.Interfaces.Execute;

namespace AKAVER_Server.Classes.Execution
{
    public class ExecutionValidationResult : IExecutionValidationResult
    {
        public bool IsValid             { get; set; }
        public string Message           { get; set; } = string.Empty;
        public string? PythonPath       { get; set; }
        public string? PipCommand       { get; set; }
        public DateTime ValidationTime  { get; set; }
    }
}
