namespace AKAVER_Server.Interfaces.IFileInfo
{
    public interface IFileInfoDto
    {
        string FileName     { get; set; }
        string ContentType  { get; set; }
        long Size           { get; set; }
        DateTime UploadDate { get; set; }
    }
}