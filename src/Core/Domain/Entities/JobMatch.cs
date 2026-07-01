using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobSearchAi.Core.Domain.Entities
{
    public class JobMatch
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid JobPostingId { get; set; }

        [ForeignKey(nameof(JobPostingId))]
        public JobPosting? JobPosting { get; set; }

        public int Score { get; set; }
        public string? Recommendation { get; set; }
        public string? MatchSummary { get; set; }
        public string? Keywords { get; set; }
        public WorkflowState State { get; set; } = WorkflowState.Discovered;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationDraft? Draft { get; set; }
        public ICollection<ApprovalDecision> ApprovalDecisions { get; set; } = new List<ApprovalDecision>();
    }

    public enum WorkflowState
    {
        Discovered,
        Scored,
        ApprovedToPrepare,
        Prepared,
        ApprovedToSubmit,
        Submitted,
        Rejected
    }
}
