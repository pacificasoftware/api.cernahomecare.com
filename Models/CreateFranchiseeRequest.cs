namespace Models;
public class CreateFranchiseeRequest
{
    public string FranchiseeName { get; set; } = "";
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? CareersEmail { get; set; }
    public string? Phone { get; set; }
    public string? TollFreePhone { get; set; }

    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public string? Slug { get; set; }
    public string? HeroImageUrl { get; set; }
    public string? CoverageTitle { get; set; }
    public string? CoverageAreas { get; set; }

    public string? PageTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? ShortDescription { get; set; }

    public bool IsPublished { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

public class UpdateFranchiseeRequest
{
    public int FranchiseeId { get; set; }

    public string FranchiseeName { get; set; } = "";
    public string? ContactName { get; set; }
    public string? Email { get; set; } 
    public string? CareersEmail { get; set; }
    public string? Phone { get; set; }
    public string? TollFreePhone { get; set; }

    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public string? Slug { get; set; }
    public string? HeroImageUrl { get; set; }
    public string? CoverageTitle { get; set; }
    public string? CoverageAreas { get; set; }

    public string? PageTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? ShortDescription { get; set; }

    public bool IsPublished { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}