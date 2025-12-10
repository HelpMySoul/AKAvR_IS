namespace AKAvR_IS.Classes.Execution
{
    public class ExecutionValidationResult
    {
        public bool IsValid             { get; set; }
        public string Message           { get; set; } = string.Empty;
        public string? PythonPath       { get; set; }
        public string? PipCommand       { get; set; }
        public DateTime ValidationTime  { get; set; }
    }
}
