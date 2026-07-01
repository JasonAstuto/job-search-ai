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
  state: string;
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

const apiBaseUrl = import.meta.env.DEV ? 'http://localhost:5000' : '';

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

function App() {
  const [postings, setPostings] = useState<JobPostingResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [form, setForm] = useState<JobPostingRequest>({
    source: 'LinkedIn',
    sourceJobId: '',
  });
  const [error, setError] = useState<string | null>(null);

  const fetchPostings = async () => {
    setIsLoading(true);
    setError(null);

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
                    {posting.matches.slice(0, 2).map((match) => (
                      <Box key={match.id} sx={{ mb: 1, p: 1, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                        <Typography variant="subtitle2">Match score: {match.score}</Typography>
                        <Typography variant="body2" color="text.secondary">
                          State: {match.state}
                        </Typography>
                        <Typography variant="body2">{match.matchSummary}</Typography>
                      </Box>
                    ))}
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
