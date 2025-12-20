using AKAVER_Server.Interfaces.Execute;

public class LibraryInstallationResult : ILibraryInstallationResult 
{ 
    public required bool Success                            { get; set; }
    public required string Message                          { get; set; }
    public required IEnumerable<string> InstalledLibraries  { get; set; }
    public required IEnumerable<string> FailedLibraries     { get; set; }
    public string? PythonPath                               { get; set; }
    public string? PipCommand                               { get; set; }
    public required DateTime InstallationTime               { get; set; }
}