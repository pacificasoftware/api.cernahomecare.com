namespace CernaHomeCare.AdminApi.Models;

public class CandidateFile
{
    public int CandidateFileId { get; set; }
    public int CandidateId { get; set; }
    public string FileName { get; set; } = "";
    public string? OriginalFileName { get; set; }
    public string FilePath { get; set; } = "";
    public string? FileContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime UploadedUtc { get; set; }

    public Candidate? Candidate { get; set; }
}