using AKAvR_IS.Interfaces.IFileInfo;

namespace AKAvR_IS.Classes.FileInfo
{
    internal class FileStorageConfig : IFileStorageConfig
    {
        public string BasePath { get; set; }       = string.Empty;
        public string ExamplesFolder { get; set; } = "examples";
    }
}