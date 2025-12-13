using AKAvR_IS.Interfaces.IPythonExecutorResult;

namespace AKAvR_IS.Classes.PythonExecutorResult
{
    internal class CheckInstalledResult : ICheckInstalledResult
    {
        public bool Success      { get; set; }
        public string PythonPath { get; set; }
        public string Output     { get; set; }
        public string Error      { get; set; }
        public int ExitCode      { get; set; }
        public string Message    { get; set; }
    }
}