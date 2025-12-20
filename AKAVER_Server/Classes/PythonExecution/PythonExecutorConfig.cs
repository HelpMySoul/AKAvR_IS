using AKAVER_Server.Interfaces.IPythonExecutor;
using System.Text;

namespace AKAVER_Server.Classes.PythonExecution
{
    public class PythonExecutorConfig : IPythonExecutorConfig
    {
        public string FileName         { get; set; } = "file_name";
        public string PythonPath       { get; set; } = "python3";
        public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();
        public string CsvInputFolder   { get; set; } = "input/";
        public string CsvOutputFolder  { get; set; } = "output/";

        public int TimeoutSeconds          { get; set; } = 300;
        public int MaxConcurrentExecutions { get; set; } = 2;

        public bool RedirectStandardOutput { get; set; } = true;
        public bool RedirectStandardError  { get; set; } = true;

        
        public Encoding OutputEncoding { get; set; } = Encoding.UTF8;

        public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);
    }

    public class PythonExecutionException : Exception
    {
        public PythonExecutionException(string message) : base(message) { }
        public PythonExecutionException(string message, Exception inner) : base(message, inner) { }
    }
}