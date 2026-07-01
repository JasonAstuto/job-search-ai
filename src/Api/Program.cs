using JobSearchAi.Api.Models;
using JobSearchAi.Core.Domain.Entities;
using JobSearchAi.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=jobsearchai;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDev", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("LocalDev");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/job-postings", async (JobPostingRequest request, AppDbContext db) =>
{
    var existing = await db.JobPostings
        .Include(p => p.Matches)
        .FirstOrDefaultAsync(p => p.Source == request.Source && p.SourceJobId == request.SourceJobId);

    if (existing is not null)
    {
        return Results.Conflict(new { Message = "Job posting already exists.", existing.Id });
    }

    var posting = new JobPosting
    {
        Source = request.Source,
        SourceJobId = request.SourceJobId,
        Title = request.Title,
        Company = request.Company,
        Location = request.Location,
        Url = request.Url,
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
    db.JobPostings.Add(posting);
    await db.SaveChangesAsync();

    var response = CreateJobPostingResponse(posting);
    return Results.Created($"/job-postings/{posting.Id}", response);
})
.WithName("IngestJobPosting")
.WithOpenApi();

app.MapGet("/job-postings", async (AppDbContext db) =>
{
    var postings = await db.JobPostings
        .Include(p => p.Matches)
        .ThenInclude(m => m.Draft)
        .AsNoTracking()
        .ToListAsync();

    return postings.Select(CreateJobPostingResponse);
})
.WithName("ListJobPostings")
.WithOpenApi();

app.MapGet("/job-matches", async (AppDbContext db) =>
{
    var matches = await db.JobMatches
        .Include(m => m.Draft)
        .AsNoTracking()
        .ToListAsync();

    return matches.Select(CreateJobMatchResponse);
})
.WithName("ListJobMatches")
.WithOpenApi();

app.MapPost("/job-matches/{matchId:guid}/drafts", async (Guid matchId, ApplicationDraftRequest request, AppDbContext db) =>
{
    var match = await db.JobMatches
        .Include(m => m.Draft)
        .FirstOrDefaultAsync(m => m.Id == matchId);

    if (match is null)
    {
        return Results.NotFound(new { Message = "Job match not found." });
    }

    if (match.Draft is not null)
    {
        return Results.Conflict(new { Message = "An application draft already exists for this match." });
    }

    if (match.State != WorkflowState.Scored && match.State != WorkflowState.ApprovedToPrepare)
    {
        return Results.BadRequest(new { Message = "Draft creation is only allowed for scored job matches." });
    }

    var draft = new ApplicationDraft
    {
        JobMatchId = match.Id,
        ResumePath = request.ResumePath,
        CoverLetterPath = request.CoverLetterPath,
        ApplicationNotes = request.ApplicationNotes,
        DraftedAt = DateTime.UtcNow,
        ApprovedToPrepareAt = DateTime.UtcNow
    };

    match.Draft = draft;
    match.State = WorkflowState.ApprovedToPrepare;
    match.UpdatedAt = DateTime.UtcNow;

    db.ApplicationDrafts.Add(draft);
    await db.SaveChangesAsync();

    return Results.Created($"/job-matches/{match.Id}/drafts/{draft.Id}", CreateApplicationDraftResponse(draft));
})
.WithName("CreateApplicationDraft")
.WithOpenApi();

app.MapPost("/job-matches/{matchId:guid}/approval-decisions", async (Guid matchId, ApprovalDecisionRequest request, AppDbContext db) =>
{
    var match = await db.JobMatches
        .Include(m => m.Draft)
        .Include(m => m.ApprovalDecisions)
        .FirstOrDefaultAsync(m => m.Id == matchId);

    if (match is null)
    {
        return Results.NotFound(new { Message = "Job match not found." });
    }

    if (request.Action == ApprovalAction.PrepareApproved && match.Draft is null)
    {
        return Results.BadRequest(new { Message = "A draft must exist before approval can be granted." });
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

    if (request.Action == ApprovalAction.PrepareApproved)
    {
        match.State = WorkflowState.Prepared;
        match.Draft!.PreparedAt = DateTime.UtcNow;
        match.UpdatedAt = DateTime.UtcNow;
    }
    else if (request.Action == ApprovalAction.PrepareRejected)
    {
        match.State = WorkflowState.Rejected;
        match.UpdatedAt = DateTime.UtcNow;
    }
    else if (request.Action == ApprovalAction.SubmitApproved)
    {
        match.State = WorkflowState.ApprovedToSubmit;
        match.UpdatedAt = DateTime.UtcNow;
    }
    else if (request.Action == ApprovalAction.SubmitRejected)
    {
        match.State = WorkflowState.Rejected;
        match.UpdatedAt = DateTime.UtcNow;
    }

    db.ApprovalDecisions.Add(decision);
    await db.SaveChangesAsync();

    return Results.Ok(new { Message = "Approval decision recorded.", matchId = match.Id, match.State });
})
.WithName("RecordApprovalDecision")
.WithOpenApi();

app.Run();

static JobPostingResponse CreateJobPostingResponse(JobPosting posting)
{
    return new JobPostingResponse(
        posting.Id,
        posting.Source,
        posting.SourceJobId,
        posting.Title,
        posting.Company,
        posting.Location,
        posting.Url,
        posting.PostedAt,
        posting.RawContent,
        posting.RawMetadata,
        posting.CreatedAt,
        posting.UpdatedAt,
        posting.Matches.Select(CreateJobMatchResponse).ToList());
}

static JobMatchResponse CreateJobMatchResponse(JobMatch match)
{
    return new JobMatchResponse(
        match.Id,
        match.JobPostingId,
        match.Score,
        match.Recommendation,
        match.MatchSummary,
        match.Keywords,
        match.State,
        match.CreatedAt,
        match.UpdatedAt,
        match.Draft is null ? null : CreateApplicationDraftResponse(match.Draft));
}

static ApplicationDraftResponse CreateApplicationDraftResponse(ApplicationDraft draft)
{
    return new ApplicationDraftResponse(
        draft.Id,
        draft.JobMatchId,
        draft.ResumePath,
        draft.CoverLetterPath,
        draft.ApplicationNotes,
        draft.DraftedAt,
        draft.PreparedAt,
        draft.ApprovedToPrepareAt);
}

static int ComputeMatchScore(JobPostingRequest request)
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

static string GenerateRecommendation(JobPostingRequest request)
{
    return request.Location?.Contains("remote", StringComparison.OrdinalIgnoreCase) == true
        ? "Remote-friendly posting; review for quick application potential."
        : "Review candidate fit and prepare draft if interest is confirmed.";
}

static string GenerateMatchSummary(JobPostingRequest request)
{
    return request.Title is not null && request.Company is not null
        ? $"{request.Title} at {request.Company} was auto-scored and is ready for draft preparation."
        : "Job posting was auto-scored and is ready for draft preparation.";
}

static string GenerateKeywords(JobPostingRequest request)
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

public partial class Program
{
}
