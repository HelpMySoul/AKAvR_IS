namespace AKAvR_IS.Interfaces.Execute
{
    public interface IExecutionValidationResult
    {
        bool IsValid             { get; set; }
        string Message           { get; set; }
        string? PythonPath       { get; set; }
        string? PipCommand       { get; set; }
        DateTime ValidationTime  { get; set; }
    }
}