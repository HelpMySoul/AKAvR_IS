using System.Text;

namespace AKAVER_Server.Interfaces.IPythonExecutor
{
    public interface IPythonExecutorConfig
    {
        public string FileName { get; set; }
        public string WorkingDirectory { get; set; }

        public string CsvInputFolder { get; set; }
        public string CsvOutputFolder { get; set; }

        public string PythonPath { get; set; }

        public int TimeoutSeconds { get; set; }
        public int MaxConcurrentExecutions { get; set; }

        public bool RedirectStandardOutput { get; set; }
        public bool RedirectStandardError { get; set; }


        public Encoding OutputEncoding { get; set; }

        public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);
    }
}