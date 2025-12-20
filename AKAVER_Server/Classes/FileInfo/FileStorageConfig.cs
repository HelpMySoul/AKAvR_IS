using AKAVER_Server.Interfaces.IFileInfo;

namespace AKAVER_Server.Classes.FileInfo
{
    public class FileStorageConfig : IFileStorageConfig
    {
        public string BasePath     { get; set; }   = "";
        public string InputFolder  { get; set; }   = "input";
        public string OutputFolder { get; set; }   = "output";
    }
}