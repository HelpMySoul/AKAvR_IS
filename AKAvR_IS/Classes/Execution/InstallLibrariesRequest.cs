using System.ComponentModel.DataAnnotations;

public class InstallLibrariesRequest
{
    [Required]
    [MinLength(1)]
    public IEnumerable<string>? Libraries { get; set; }

    public Dictionary<string, string> LibraryVersions { get; set; } = new Dictionary<string, string>();

    public string ExtraPipOptions { get; set; } = string.Empty;
}