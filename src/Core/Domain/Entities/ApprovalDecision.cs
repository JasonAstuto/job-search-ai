using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobSearchAi.Core.Domain.Entities
{
    public class ApprovalDecision
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid JobMatchId { get; set; }

        [ForeignKey(nameof(JobMatchId))]
        public JobMatch? JobMatch { get; set; }

        public ApprovalAction Action { get; set; }
        public string? Notes { get; set; }
        public string? Actor { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum ApprovalAction
    {
        PrepareRequested,
        PrepareApproved,
        PrepareRejected,
        SubmitRequested,
        SubmitApproved,
        SubmitRejected
    }
}
