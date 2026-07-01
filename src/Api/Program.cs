using JobSearchAi.Api.Models;
using JobSearchAi.Api.Services;
using JobSearchAi.Core.Domain.Entities;
using JobSearchAi.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=jobsearchai;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IJobWorkflowService, JobWorkflowService>();
builder.Services.AddHealthChecks();
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

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
   .WithName("HealthCheck")
   .WithOpenApi();

app.MapPost("/job-postings", async (JobPostingRequest request, IJobWorkflowService workflowService) =>
{
    try
    {
        var posting = await workflowService.IngestJobPostingAsync(request);
        var response = CreateJobPostingResponse(posting);
        return Results.Created($"/job-postings/{posting.Id}", response);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Conflict(new { Message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Message = ex.Message, Errors = new Dictionary<string, string[]> { ["request"] = [ex.Message] } });
    }
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

app.MapPost("/job-matches/{matchId:guid}/drafts", async (Guid matchId, ApplicationDraftRequest request, IJobWorkflowService workflowService) =>
{
    try
    {
        var match = await workflowService.CreateDraftAsync(matchId, request);
        return Results.Created($"/job-matches/{match.Id}/drafts/{match.Draft!.Id}", CreateApplicationDraftResponse(match.Draft));
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { Message = "Job match not found." });
    }
    catch (InvalidOperationException ex)
    {
        return ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
            ? Results.Conflict(new { Message = ex.Message })
            : Results.BadRequest(new { Message = ex.Message });
    }
})
.WithName("CreateApplicationDraft")
.WithOpenApi();

app.MapPost("/job-matches/{matchId:guid}/approval-decisions", async (Guid matchId, ApprovalDecisionRequest request, IJobWorkflowService workflowService) =>
{
    try
    {
        var match = await workflowService.RecordApprovalDecisionAsync(matchId, request);
        return Results.Ok(new { Message = "Approval decision recorded.", matchId = match.Id, state = (int)match.State });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { Message = "Job match not found." });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Message = ex.Message });
    }
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

public partial class Program
{
}
