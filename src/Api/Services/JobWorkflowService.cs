using JobSearchAi.Api.Models;
using JobSearchAi.Core.Domain.Entities;
using JobSearchAi.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobSearchAi.Api.Services;

public sealed class JobWorkflowService : IJobWorkflowService
{
    private readonly AppDbContext _dbContext;

    public JobWorkflowService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<JobPosting> IngestJobPostingAsync(JobPostingRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateJobPostingRequest(request);
        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException("Job posting request is invalid.");
        }

        var existing = await _dbContext.JobPostings
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Source == request.Source && p.SourceJobId == request.SourceJobId, cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException("Job posting already exists.");
        }

        var posting = new JobPosting
        {
            Source = request.Source.Trim(),
            SourceJobId = request.SourceJobId.Trim(),
            Title = request.Title?.Trim(),
            Company = request.Company?.Trim(),
            Location = request.Location?.Trim(),
            Url = request.Url?.Trim(),
            PostedAt = request.PostedAt,
            RawContent = request.RawContent,
            RawMetadata = request.RawMetadata,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var match = new JobMatch
        {
            JobPosting = posting,
            Score = ComputeMatchScore(request),
            Recommendation = GenerateRecommendation(request),
            MatchSummary = GenerateMatchSummary(request),
            Keywords = GenerateKeywords(request),
            State = WorkflowState.Scored,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        posting.Matches.Add(match);
        _dbContext.JobPostings.Add(posting);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return posting;
    }

    public async Task<JobMatch> CreateDraftAsync(Guid matchId, ApplicationDraftRequest request, CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.JobMatches
            .Include(m => m.Draft)
            .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);

        if (match is null)
        {
            throw new KeyNotFoundException("Job match not found.");
        }

        if (match.Draft is not null)
        {
            throw new InvalidOperationException("An application draft already exists for this match.");
        }

        if (match.State != WorkflowState.Scored && match.State != WorkflowState.ApprovedToPrepare)
        {
            throw new InvalidOperationException("Draft creation is only allowed for scored job matches.");
        }

        var draft = new ApplicationDraft
        {
            JobMatchId = match.Id,
            ResumePath = request.ResumePath?.Trim(),
            CoverLetterPath = request.CoverLetterPath?.Trim(),
            ApplicationNotes = request.ApplicationNotes,
            DraftedAt = DateTime.UtcNow,
            ApprovedToPrepareAt = DateTime.UtcNow
        };

        match.Draft = draft;
        match.State = WorkflowState.ApprovedToPrepare;
        match.UpdatedAt = DateTime.UtcNow;

        _dbContext.ApplicationDrafts.Add(draft);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return match;
    }

    public async Task<JobMatch> RecordApprovalDecisionAsync(Guid matchId, ApprovalDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.JobMatches
            .Include(m => m.Draft)
            .Include(m => m.ApprovalDecisions)
            .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);

        if (match is null)
        {
            throw new KeyNotFoundException("Job match not found.");
        }

        if (request.Action == ApprovalAction.PrepareApproved && match.Draft is null)
        {
            throw new InvalidOperationException("A draft must exist before approval can be granted.");
        }

        if (request.Action == ApprovalAction.PrepareApproved && match.State != WorkflowState.ApprovedToPrepare)
        {
            throw new InvalidOperationException("Prepare approval is only available once a draft has been created.");
        }

        if (request.Action == ApprovalAction.SubmitApproved && match.State != WorkflowState.Prepared)
        {
            throw new InvalidOperationException("Submit approval is only available after preparation has been approved.");
        }

        var decision = new ApprovalDecision
        {
            JobMatchId = match.Id,
            Action = request.Action,
            Notes = request.Notes,
            Actor = request.Actor,
            CreatedAt = DateTime.UtcNow
        };

        match.ApprovalDecisions.Add(decision);

        switch (request.Action)
        {
            case ApprovalAction.PrepareApproved:
                match.State = WorkflowState.Prepared;
                match.Draft!.PreparedAt = DateTime.UtcNow;
                match.UpdatedAt = DateTime.UtcNow;
                break;
            case ApprovalAction.PrepareRejected:
                match.State = WorkflowState.Rejected;
                match.UpdatedAt = DateTime.UtcNow;
                break;
            case ApprovalAction.SubmitApproved:
                match.State = WorkflowState.ApprovedToSubmit;
                match.UpdatedAt = DateTime.UtcNow;
                break;
            case ApprovalAction.SubmitRejected:
                match.State = WorkflowState.Rejected;
                match.UpdatedAt = DateTime.UtcNow;
                break;
        }

        _dbContext.ApprovalDecisions.Add(decision);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return match;
    }

    private static Dictionary<string, string[]> ValidateJobPostingRequest(JobPostingRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Source))
        {
            errors[nameof(request.Source)] = ["Source is required."];
        }

        if (string.IsNullOrWhiteSpace(request.SourceJobId))
        {
            errors[nameof(request.SourceJobId)] = ["SourceJobId is required."];
        }

        return errors;
    }

    private static int ComputeMatchScore(JobPostingRequest request)
    {
        var score = 45;

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            score += 10;
        }

        if (!string.IsNullOrWhiteSpace(request.Company))
        {
            score += 10;
        }

        if (!string.IsNullOrWhiteSpace(request.Location) && request.Location.Contains("remote", StringComparison.OrdinalIgnoreCase))
        {
            score += 15;
        }

        if (!string.IsNullOrWhiteSpace(request.RawContent))
        {
            score += Math.Min(20, request.RawContent.Length / 200);
        }

        return Math.Clamp(score, 0, 100);
    }

    private static string GenerateRecommendation(JobPostingRequest request)
    {
        return request.Location?.Contains("remote", StringComparison.OrdinalIgnoreCase) == true
            ? "Remote-friendly posting; review for quick application potential."
            : "Review candidate fit and prepare draft if interest is confirmed.";
    }

    private static string GenerateMatchSummary(JobPostingRequest request)
    {
        return request.Title is not null && request.Company is not null
            ? $"{request.Title} at {request.Company} was auto-scored and is ready for draft preparation."
            : "Job posting was auto-scored and is ready for draft preparation.";
    }

    private static string GenerateKeywords(JobPostingRequest request)
    {
        var keywords = new List<string?>
        {
            request.Title,
            request.Company,
            request.Location,
            request.RawContent
        }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .SelectMany(value => value!.Split(new[] { ' ', ',', ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        .Select(token => token.Trim())
        .Where(token => token.Length > 3)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(12)
        .ToArray();

        return string.Join(", ", keywords);
    }
}
