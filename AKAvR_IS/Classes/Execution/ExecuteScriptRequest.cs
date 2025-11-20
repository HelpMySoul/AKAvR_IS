using System.ComponentModel.DataAnnotations;

namespace AKAvR_IS.Classes.Execution
{
    public class ExecuteScriptRequest
    {
        [Required]
        public string ScriptPath { get; set; } = string.Empty;

        public string? PythonPath { get; set; }
        public string? WorkingDirectory { get; set; }
        public int? Timeout { get; set; } // в секундах
        public Dictionary<string, object>? Parameters { get; set; }
    }
}
