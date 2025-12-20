namespace AKAvR_IS.Interfaces.IPythonExecutor
{
    // Интерфейс для управления Python окружением
    public interface IPythonEnvironmentManager 
    { 
        bool InstallPackage(string packageName);
        bool CheckPackageInstalled(string packageName); 
        string[] GetInstalledPackages(); 
    }
}
