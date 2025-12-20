using System.ComponentModel.DataAnnotations;

namespace AKAVER_Server.Classes.Execution.Batch
{
    public class BatchExecuteRequest
    {
        [Required]
        [MinLength(1)]
        public List<ExecuteScriptRequest> ScriptRequests { get; set; } = new List<ExecuteScriptRequest>();
    }
}
