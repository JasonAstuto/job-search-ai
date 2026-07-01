using System.ComponentModel.DataAnnotations;

namespace JobSearchAi.Core.Domain.Entities
{
    public class JobPosting
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Source { get; set; } = null!;

        [Required]
        public string SourceJobId { get; set; } = null!;

        public string? Title { get; set; }
        public string? Company { get; set; }
        public string? Location { get; set; }
        public string? Url { get; set; }
        public DateTime? PostedAt { get; set; }
        public string? RawContent { get; set; }
        public string? RawMetadata { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<JobMatch> Matches { get; set; } = new List<JobMatch>();
    }
}
