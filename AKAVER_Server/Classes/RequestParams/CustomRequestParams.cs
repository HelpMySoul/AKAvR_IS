using AKAVER_Server.Interfaces.IPythonExecutor;

namespace AKAVER_Server.Classes.RequestParams
{
    public class CustomRequestParams : IRequestParam
    {
        public required string ParameterName { get; set; }
        public object Value { get; set; } = string.Empty;

        public Type ValueType => Value?.GetType() ?? typeof(object);
    }
}
