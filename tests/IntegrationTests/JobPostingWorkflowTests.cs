using System.Net;
using System.Net.Http.Json;
using JobSearchAi.Api.Models;
using JobSearchAi.Core.Domain.Entities;
using JobSearchAi.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace JobSearchAi.IntegrationTests;

public sealed class JobPostingWorkflowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public JobPostingWorkflowTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                    services.RemoveAll(typeof(AppDbContext));
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("workflow-tests"));
                });
            })
            .CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(payload);
        Assert.Equal("Healthy", payload!["status"]?.ToString());
    }

    [Fact]
    public async Task IngestingPostingWithoutRequiredFields_ReturnsValidationError()
    {
        var postingRequest = new JobPostingRequest(
            "",
            "",
            "Principal Engineer",
            "Contoso",
            "Remote",
            "https://example.com/jobs/invalid",
            null,
            "A sample posting",
            null);

        var response = await _client.PostAsJsonAsync("/job-postings", postingRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DraftAndApprovalWorkflow_CompletesEndToEnd()
    {
        var postingRequest = new JobPostingRequest(
            "LinkedIn",
            "workflow-123",
            "Principal Engineer",
            "Contoso",
            "Remote",
            "https://example.com/jobs/workflow-123",
            null,
            "A sample posting",
            null);

        var postingResponse = await _client.PostAsJsonAsync("/job-postings", postingRequest);
        Assert.Equal(HttpStatusCode.Created, postingResponse.StatusCode);

        var posting = await postingResponse.Content.ReadFromJsonAsync<JobPostingResponse>();
        Assert.NotNull(posting);
        Assert.Single(posting.Matches);

        var matchId = posting.Matches[0].Id;
        var draftRequest = new ApplicationDraftRequest(
            "/tmp/resume.pdf",
            "/tmp/cover-letter.pdf",
            "Tailor for the team's platform work.");

        var draftResponse = await _client.PostAsJsonAsync($"/job-matches/{matchId}/drafts", draftRequest);
        Assert.Equal(HttpStatusCode.Created, draftResponse.StatusCode);

        var approvalRequest = new ApprovalDecisionRequest(
            ApprovalAction.PrepareApproved,
            "Approved for preparation.",
            "test-user");

        var approvalResponse = await _client.PostAsJsonAsync($"/job-matches/{matchId}/approval-decisions", approvalRequest);
        Assert.Equal(HttpStatusCode.OK, approvalResponse.StatusCode);

        var approvalPayload = await approvalResponse.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(approvalPayload);
        Assert.True(approvalPayload!.ContainsKey("state") || approvalPayload.ContainsKey("State"));
        var serializedState = approvalPayload.ContainsKey("state")
            ? approvalPayload["state"]?.ToString()
            : approvalPayload["State"]?.ToString();
        Assert.Equal(((int)WorkflowState.Prepared).ToString(), serializedState);
    }
}
