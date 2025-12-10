namespace AKAvR_IS.Classes.Execution
{
    public class PipStatusResult
    {
        public bool IsPipAvailable      { get; set; }
        public required string Message  { get; set; }
        public DateTime CheckTime       { get; set; }
        public string? PythonPath       { get; set; } 
        public string? PipCommand       { get; set; } 
    }
}
