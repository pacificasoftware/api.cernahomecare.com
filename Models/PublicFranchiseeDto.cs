namespace Models;
public class PublicFranchiseeDto
{
    public int FranchiseeId { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Phone { get; set; } = "";
    public string PhoneHref { get; set; } = "";
    public string? JobsZip { get; set; }
}
 
