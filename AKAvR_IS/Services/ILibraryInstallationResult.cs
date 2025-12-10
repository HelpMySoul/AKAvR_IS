namespace AKAvR_IS.Services
{   
    public interface ILibraryInstallationResult
    {
        bool Success { get; set; }
        string Message { get; set; }
        IEnumerable<string> InstalledLibraries { get; set; }
        IEnumerable<string> FailedLibraries { get; set; }
        DateTime InstallationTime { get; set; }
    }
}