namespace AKAVER_Server.Interfaces.IFileInfo
{
    public interface IFileStorageConfig
    {
        string BasePath { get; set; }
        string InputFolder  { get; set; }
        string OutputFolder { get; set; }
    }
}