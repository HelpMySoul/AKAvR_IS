using AKAvR_IS.Interfaces.IPythonExecutor;

namespace AKAvR_IS.Classes.RequestParams
{
    public class ScriptPathRequestParams : IRequestParam
    {
        public string ScriptPath { get; set; } = string.Empty;

        public string ParameterName => "scriptPath";
        public object Value => ScriptPath;
        public Type ValueType => typeof(string);
    }
}
