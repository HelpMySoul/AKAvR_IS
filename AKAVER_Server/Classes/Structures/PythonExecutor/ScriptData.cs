using AKAVER_Server.Interfaces.IPythonExecutor;

namespace AKAVER_Server.Classes.Structures.PythonExecutor
{
    public struct ScriptData
    {
        public string ScriptName { get; }
        public IEnumerable<IRequestParam> Parameters { get; set; }

        public ScriptData(string scriptName, IEnumerable<IRequestParam> parameters)
        {
            ScriptName = scriptName;
            Parameters = parameters;            
        }
    }
}