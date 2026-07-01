using JobSearchAi.Core.Domain.Entities;

namespace JobSearchAi.Api.Models;

public record JobPostingRequest(
    string Source,
    string SourceJobId,
    string? Title,
    string? Company,
    string? Location,
    string? Url,
    DateTime? PostedAt,
    string? RawContent,
    string? RawMetadata);

public record ApplicationDraftRequest(
    string? ResumePath,
    string? CoverLetterPath,
    string? ApplicationNotes);

public record ApprovalDecisionRequest(
    ApprovalAction Action,
    string? Notes,
    string? Actor);

public record JobPostingResponse(
    Guid Id,
    string Source,
    string SourceJobId,
    string? Title,
    string? Company,
    string? Location,
    string? Url,
    DateTime? PostedAt,
    string? RawContent,
    string? RawMetadata,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<JobMatchResponse> Matches);

public record JobMatchResponse(
    Guid Id,
    Guid JobPostingId,
    int Score,
    string? Recommendation,
    string? MatchSummary,
    string? Keywords,
    WorkflowState State,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ApplicationDraftResponse? Draft);

public record ApplicationDraftResponse(
    Guid Id,
    Guid JobMatchId,
    string? ResumePath,
    string? CoverLetterPath,
    string? ApplicationNotes,
    DateTime DraftedAt,
    DateTime? PreparedAt,
    DateTime? ApprovedToPrepareAt);
