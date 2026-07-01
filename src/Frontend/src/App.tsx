import { Box, Button, Container, Typography } from '@mui/material';

function App() {
  return (
    <Container maxWidth="lg" sx={{ py: 6 }}>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
        <Box>
          <Typography variant="h3" component="h1" gutterBottom>
            AI Executive Job Search Platform
          </Typography>
          <Typography variant="body1" color="text.secondary">
            A production-minded workflow for discovering, scoring, and preparing executive leadership applications.
          </Typography>
        </Box>

        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
          <Button variant="outlined">Workflow-governed</Button>
          <Button variant="outlined">Human approval required</Button>
          <Button variant="outlined">Public alerts only</Button>
        </Box>

        <Box sx={{ p: 3, backgroundColor: 'background.paper', borderRadius: 2, boxShadow: 1 }}>
          <Typography variant="h5" gutterBottom>
            Phase 1 scaffold complete
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            The repository now contains the initial solution, API, worker, Playwright worker, core libraries, tests, and a frontend shell.
          </Typography>
          <Button variant="contained">Review workflow</Button>
        </Box>
      </Box>
    </Container>
  );
}

export default App;
