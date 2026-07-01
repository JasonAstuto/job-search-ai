using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobSearchAi.Core.Domain.Entities
{
    public class ApplicationDraft
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid JobMatchId { get; set; }

        [ForeignKey(nameof(JobMatchId))]
        public JobMatch? JobMatch { get; set; }

        public string? ResumePath { get; set; }
        public string? CoverLetterPath { get; set; }
        public string? ApplicationNotes { get; set; }
        public DateTime DraftedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PreparedAt { get; set; }
        public DateTime? ApprovedToPrepareAt { get; set; }
    }
}
