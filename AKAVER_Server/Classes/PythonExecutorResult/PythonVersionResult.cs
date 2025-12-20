using AKAVER_Server.Interfaces.IPythonExecutorResult;

namespace AKAVER_Server.Classes.PythonExecutorResult
{
    internal class PythonVersionResult : IPythonVersionResult
    {
        public bool Success         { get; set; }
        public string PythonPath    { get; set; }
        public string VersionOutput { get; set; }
        public string Error         { get; set; }
        public int ExitCode         { get; set; }
        public string Message       { get; set; }
    }
}