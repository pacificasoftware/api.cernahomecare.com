namespace  Models
{
    public class CernaFranchiseeEmailResponse
    {
        public int FranchiseeId { get; set; }

        public string? FranchiseeName { get; set; }

        public string? Email { get; set; }

        public string? CareersEmail { get; set; }

        public bool IsActive { get; set; }
    }
}
