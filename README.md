# Job Search AI

A production-minded platform for AI-assisted executive job search, scoring, and application preparation.

## Repository structure

- src/Api: ASP.NET Core API host
- src/Workers: background worker service
- src/PlaywrightWorker: browser automation worker
- src/Core: shared domain, application, and infrastructure layers
- src/Frontend: React + Material UI dashboard shell
- tests: unit and integration test projects
- docs: architecture, ADRs, and implementation planning

## Getting started

- Backend: dotnet build JobSearchAi.sln
- Frontend: cd src/Frontend && npm install && npm run dev

## Runtime requirements

- .NET SDK: 10.x
- Node.js: 26.x (or at least 22.12+)
- npm: 10.x or later
