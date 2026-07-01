import { useEffect, useState, type FormEvent } from 'react';
import {
  Box,
  Button,
  Card,
  CardActions,
  CardContent,
  Container,
  Divider,
  TextField,
  Typography,
} from '@mui/material';

type JobPostingRequest = {
  source: string;
  sourceJobId: string;
  title?: string;
  company?: string;
  location?: string;
  url?: string;
  postedAt?: string;
  rawContent?: string;
  rawMetadata?: string;
};

type DraftInputState = {
  resumePath: string;
  coverLetterPath: string;
  applicationNotes: string;
  notes: string;
};

type ApprovalAction = 'PrepareApproved' | 'PrepareRejected' | 'SubmitApproved' | 'SubmitRejected';

type ApplicationDraftResponse = {
  id: string;
  jobMatchId: string;
  resumePath?: string;
  coverLetterPath?: string;
  applicationNotes?: string;
  draftedAt: string;
  preparedAt?: string;
  approvedToPrepareAt?: string;
};

type JobMatchResponse = {
  id: string;
  jobPostingId: string;
  score: number;
  recommendation?: string;
  matchSummary?: string;
  keywords?: string;
  state: string | number;
  createdAt: string;
  updatedAt: string;
  draft?: ApplicationDraftResponse;
};

type JobPostingResponse = {
  id: string;
  source: string;
  sourceJobId: string;
  title?: string;
  company?: string;
  location?: string;
  url?: string;
  postedAt?: string;
  rawContent?: string;
  rawMetadata?: string;
  createdAt: string;
  updatedAt: string;
  matches: JobMatchResponse[];
};

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';

function deriveSourceJobId(url: string | undefined, sourceJobId: string): string {
  const trimmedSourceJobId = sourceJobId.trim();
  if (trimmedSourceJobId) {
    return trimmedSourceJobId;
  }

  const trimmedUrl = url?.trim();
  if (!trimmedUrl) {
    return '';
  }

  try {
    const parsedUrl = new URL(trimmedUrl);
    const currentJobId = parsedUrl.searchParams.get('currentJobId')
      ?? parsedUrl.searchParams.get('jobId')
      ?? parsedUrl.searchParams.get('id');

    if (currentJobId) {
      return currentJobId;
    }

    const lastSegment = parsedUrl.pathname.split('/').filter(Boolean).pop();
    return lastSegment ?? '';
  } catch {
    return '';
  }
}

function getWorkflowStateLabel(state: string | number): string {
  switch (String(state)) {
    case '0':
    case 'Discovered':
      return 'Discovered';
    case '1':
    case 'Scored':
      return 'Scored';
    case '2':
    case 'ApprovedToPrepare':
      return 'Approved to prepare';
    case '3':
    case 'Prepared':
      return 'Prepared';
    case '4':
    case 'ApprovedToSubmit':
      return 'Approved to submit';
    case '5':
    case 'Submitted':
      return 'Submitted';
    case '6':
    case 'Rejected':
      return 'Rejected';
    default:
      return `State ${state}`;
  }
}

function App() {
  const [postings, setPostings] = useState<JobPostingResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [form, setForm] = useState<JobPostingRequest>({
    source: 'LinkedIn',
    sourceJobId: '',
  });
  const [draftInputs, setDraftInputs] = useState<Record<string, DraftInputState>>({});
  const [workflowBusy, setWorkflowBusy] = useState<Record<string, boolean>>({});
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  const fetchPostings = async () => {
    setIsLoading(true);
    setError(null);
    setFeedback(null);

    try {
      const response = await fetch(`${apiBaseUrl}/job-postings`);
      if (!response.ok) {
        throw new Error(`API error: ${response.status}`);
      }

      const data = (await response.json()) as JobPostingResponse[];
      setPostings(data);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : String(caught));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchPostings();
  }, []);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setFeedback(null);

    const inferredJobId = deriveSourceJobId(form.url, form.sourceJobId);
    if (!inferredJobId) {
      setError('Enter a source job ID or paste a supported job URL so it can be inferred.');
      return;
    }

    const submission = { ...form, sourceJobId: inferredJobId };

    setIsSubmitting(true);
    try {
      const response = await fetch(`${apiBaseUrl}/job-postings`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(submission),
      });

      if (!response.ok) {
        const payload = await response.json();
        throw new Error(payload?.message ?? `API error ${response.status}`);
      }

      await fetchPostings();
      setForm({ source: 'LinkedIn', sourceJobId: inferredJobId });
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : String(caught));
    } finally {
      setIsSubmitting(false);
    }
  };

  const updateDraftInput = (matchId: string, field: keyof DraftInputState, value: string) => {
    setDraftInputs((prev) => ({
      ...prev,
      [matchId]: {
        ...(prev[matchId] ?? { resumePath: '', coverLetterPath: '', applicationNotes: '', notes: '' }),
        [field]: value,
      },
    }));
  };

  const setMatchBusy = (matchId: string, value: boolean) => {
    setWorkflowBusy((prev) => ({ ...prev, [matchId]: value }));
  };

  const handleCreateDraft = async (matchId: string) => {
    const currentInput = draftInputs[matchId] ?? { resumePath: '', coverLetterPath: '', applicationNotes: '', notes: '' };
    setMatchBusy(matchId, true);
    setError(null);
    setFeedback(null);

    try {
      const response = await fetch(`${apiBaseUrl}/job-matches/${matchId}/drafts`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          resumePath: currentInput.resumePath || undefined,
          coverLetterPath: currentInput.coverLetterPath || undefined,
          applicationNotes: currentInput.applicationNotes || undefined,
        }),
      });

      if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new Error(payload?.message ?? `API error ${response.status}`);
      }

      await fetchPostings();
      setFeedback('Draft created and queued for approval.');
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : String(caught));
    } finally {
      setMatchBusy(matchId, false);
    }
  };

  const handleApprovalDecision = async (matchId: string, action: ApprovalAction) => {
    const currentInput = draftInputs[matchId] ?? { resumePath: '', coverLetterPath: '', applicationNotes: '', notes: '' };
    setMatchBusy(matchId, true);
    setError(null);
    setFeedback(null);

    try {
      const response = await fetch(`${apiBaseUrl}/job-matches/${matchId}/approval-decisions`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          action,
          notes: currentInput.notes || undefined,
          actor: 'human-operator',
        }),
      });

      if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new Error(payload?.message ?? `API error ${response.status}`);
      }

      await fetchPostings();
      setFeedback(action === 'PrepareApproved' ? 'Prepare approval recorded.' : 'Prepare rejection recorded.');
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : String(caught));
    } finally {
      setMatchBusy(matchId, false);
    }
  };

  return (
    <Container maxWidth="lg" sx={{ py: 6 }}>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
        <Box>
          <Typography variant="h3" component="h1" gutterBottom>
            AI Executive Job Search Platform
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Ingest job postings, review scored matches, and prepare application drafts with human approval controls.
          </Typography>
        </Box>

        <Box>
          <Typography variant="h5" gutterBottom>
            Ingest a new job posting
          </Typography>
          <Box component="form" onSubmit={handleSubmit} sx={{ display: 'grid', gap: 2 }}>
            <Box sx={{ display: 'grid', gap: 16, gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))' }}>
              <TextField
                label="Source"
                value={form.source}
                onChange={(event) => setForm((prev) => ({ ...prev, source: event.target.value }))}
                fullWidth
                required
              />
              <TextField
                label="Source Job ID"
                value={form.sourceJobId}
                onChange={(event) => setForm((prev) => ({ ...prev, sourceJobId: event.target.value }))}
                fullWidth
                required
              />
              <TextField
                label="Title"
                value={form.title ?? ''}
                onChange={(event) => setForm((prev) => ({ ...prev, title: event.target.value }))}
                fullWidth
              />
              <TextField
                label="Company"
                value={form.company ?? ''}
                onChange={(event) => setForm((prev) => ({ ...prev, company: event.target.value }))}
                fullWidth
              />
              <TextField
                label="Location"
                value={form.location ?? ''}
                onChange={(event) => setForm((prev) => ({ ...prev, location: event.target.value }))}
                fullWidth
              />
              <TextField
                label="URL"
                value={form.url ?? ''}
                onChange={(event) => setForm((prev) => ({ ...prev, url: event.target.value }))}
                fullWidth
              />
            </Box>
            <TextField
              label="Raw Content"
              value={form.rawContent ?? ''}
              onChange={(event) => setForm((prev) => ({ ...prev, rawContent: event.target.value }))}
              fullWidth
              multiline
              minRows={3}
            />

            {error && (
              <Typography color="error" variant="body2">
                {error}
              </Typography>
            )}
            {feedback && !error && (
              <Typography color="success.main" variant="body2">
                {feedback}
              </Typography>
            )}

            <Button type="submit" variant="contained" disabled={isSubmitting} sx={{ width: 180 }}>
              {isSubmitting ? 'Submitting…' : 'Ingest now'}
            </Button>
          </Box>
        </Box>

        <Divider />

        <Box>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2, gap: 2, flexWrap: 'wrap' }}>
            <Typography variant="h5">Job postings</Typography>
            <Button variant="outlined" onClick={fetchPostings} disabled={isLoading}>
              Refresh
            </Button>
          </Box>

          {isLoading ? (
            <Typography>Loading postings…</Typography>
          ) : postings.length === 0 ? (
            <Typography color="text.secondary">No postings have been ingested yet.</Typography>
          ) : (
            <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))' }}>
              {postings.map((posting) => (
                <Card key={posting.id}>
                  <CardContent>
                    <Typography variant="h6">{posting.title || 'Untitled posting'}</Typography>
                    <Typography variant="body2" color="text.secondary" gutterBottom>
                      {posting.company ?? 'Unknown company'} · {posting.location ?? 'Unknown location'}
                    </Typography>
                    <Typography variant="body2" sx={{ mb: 1 }}>
                      Source: {posting.source} · ID: {posting.sourceJobId}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                      Matches: {posting.matches.length}
                    </Typography>
                    {posting.matches.slice(0, 2).map((match) => {
                      const draftInput = draftInputs[match.id] ?? { resumePath: '', coverLetterPath: '', applicationNotes: '', notes: '' };
                      const matchStateLabel = getWorkflowStateLabel(match.state);
                      const isBusy = workflowBusy[match.id] ?? false;

                      return (
                        <Box key={match.id} sx={{ mb: 1, p: 1, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                          <Typography variant="subtitle2">Match score: {match.score}</Typography>
                          <Typography variant="body2" color="text.secondary">
                            State: {matchStateLabel}
                          </Typography>
                          <Typography variant="body2">{match.matchSummary}</Typography>

                          {match.draft ? (
                            <Box sx={{ mt: 1, display: 'grid', gap: 1 }}>
                              <Typography variant="body2" color="text.secondary">
                                Draft prepared for {match.draft.resumePath ?? 'manual review'}.
                              </Typography>
                              <TextField
                                label="Approval notes"
                                value={draftInput.notes}
                                onChange={(event) => updateDraftInput(match.id, 'notes', event.target.value)}
                                size="small"
                                fullWidth
                              />
                              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                                <Button size="small" variant="contained" onClick={() => handleApprovalDecision(match.id, 'PrepareApproved')} disabled={isBusy}>
                                  Approve prepare
                                </Button>
                                <Button size="small" variant="outlined" onClick={() => handleApprovalDecision(match.id, 'PrepareRejected')} disabled={isBusy}>
                                  Reject prepare
                                </Button>
                              </Box>
                            </Box>
                          ) : (
                            <Box sx={{ mt: 1, display: 'grid', gap: 1 }}>
                              <TextField
                                label="Resume path"
                                value={draftInput.resumePath}
                                onChange={(event) => updateDraftInput(match.id, 'resumePath', event.target.value)}
                                size="small"
                                fullWidth
                              />
                              <TextField
                                label="Cover letter path"
                                value={draftInput.coverLetterPath}
                                onChange={(event) => updateDraftInput(match.id, 'coverLetterPath', event.target.value)}
                                size="small"
                                fullWidth
                              />
                              <TextField
                                label="Application notes"
                                value={draftInput.applicationNotes}
                                onChange={(event) => updateDraftInput(match.id, 'applicationNotes', event.target.value)}
                                size="small"
                                fullWidth
                                multiline
                                minRows={2}
                              />
                              <Button size="small" variant="contained" onClick={() => handleCreateDraft(match.id)} disabled={isBusy}>
                                Create draft
                              </Button>
                            </Box>
                          )}
                        </Box>
                      );
                    })}
                  </CardContent>
                  <CardActions>
                    {posting.url && (
                      <Button size="small" href={posting.url} target="_blank" rel="noreferrer">
                        Open source
                      </Button>
                    )}
                  </CardActions>
                </Card>
              ))}
            </Box>
          )}
        </Box>
      </Box>
    </Container>
  );
}

export default App;
