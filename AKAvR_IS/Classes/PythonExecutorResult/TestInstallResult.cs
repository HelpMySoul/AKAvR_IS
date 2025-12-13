using AKAvR_IS.Interfaces.IPythonExecutorResult;

namespace AKAvR_IS.Classes.PythonExecutorResult
{
    internal class TestInstallResult : ITestInstallResult
    {
        public bool Success         { get; set; }
        public string Message       { get; set; }
        public string PythonPath    { get; set; }
        public string PipCommand    { get; set; }
        public int ExitCode         { get; set; }
        public string Output        { get; set; }
        public string Error         { get; set; }
        public string ExceptionType { get; set; }
        public string StackTrace    { get; set; }
    }
}