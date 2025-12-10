public class PythonEnvironmentInfo
{    
    public string? PythonPath { get; set; }
    public string? PipCommand { get; set; }
    public DateTime CheckTime { get; set; }
    public bool IsAvailable { get; set; }
    public string? Error { get; set; }
}