using JobSearchAi.Api.Models;
using JobSearchAi.Core.Domain.Entities;

namespace JobSearchAi.Api.Services;

public interface IJobWorkflowService
{
    Task<JobPosting> IngestJobPostingAsync(JobPostingRequest request, CancellationToken cancellationToken = default);
    Task<JobMatch> CreateDraftAsync(Guid matchId, ApplicationDraftRequest request, CancellationToken cancellationToken = default);
    Task<JobMatch> RecordApprovalDecisionAsync(Guid matchId, ApprovalDecisionRequest request, CancellationToken cancellationToken = default);
}
