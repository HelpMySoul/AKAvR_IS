namespace AKAvR_IS.Interfaces.IFileInfo
{
    internal interface IFileStorageConfig
    {
        string BasePath       { get; set; }
        string ExamplesFolder { get; set; }
    }
}