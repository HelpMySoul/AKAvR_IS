using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AKAvR_IS.Classes.Execution
{
    public class ExecuteScriptRequest
    {
        [Required]
        public string ScriptName { get; set; } = string.Empty;
        [Required]
        public Dictionary<string, object>? Parameters { get; set; }
    }
}
