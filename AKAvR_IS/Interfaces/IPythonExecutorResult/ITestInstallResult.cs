namespace AKAvR_IS.Interfaces.IPythonExecutorResult
{
    public interface ITestInstallResult
    {
        bool Success         { get; set; }
        string Message       { get; set; }
        string PythonPath    { get; set; }
        string PipCommand    { get; set; }
        int ExitCode         { get; set; }
        string Output        { get; set; }
        string Error         { get; set; }
        string ExceptionType { get; set; }
        string StackTrace    { get; set; }
    }
}