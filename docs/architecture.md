# Architecture

## Overview

This project will be implemented as a production-minded, workflow-governed platform for AI-assisted executive job search and application preparation.

## Goals

- Discover relevant leadership roles from public job alerts
- Deduplicate and score roles against the user's background
- Tailor resumes and draft application materials without inventing unsupported content
- Present recommendations for human review and approval
- Prepare applications only after explicit approval
- Maintain a complete and auditable record of all workflow actions

## High-level components

- Frontend: React + Material UI dashboard
- API: ASP.NET Core backend for workflow orchestration and user interaction
- Worker: background service for ingestion, scoring, and drafting
- Playwright worker: isolated browser automation service for form preparation only
- Data: PostgreSQL for durable state and audit history
- Storage: S3 for generated documents and screenshots
- Platform: AWS ECS Fargate, RDS, SQS, EventBridge, Secrets Manager, CloudWatch
- IaC: AWS CDK
- CI/CD: GitHub Actions

## Core architecture principles

- Treat this as production software, not a demo
- Keep agents as constrained workers with structured inputs and outputs
- Make the workflow engine the owner of state transitions and approvals
- Never submit applications without explicit human approval
- Separate “approved to prepare” from “approved to submit”
- Avoid risky scraping practices; prefer public alerts and approved sources
- Keep secrets, resumes, credentials, and personal data out of logs

## Initial MVP scope

- Role focus: Director of Engineering, Senior Director of Engineering, Head of Engineering, Director of Software Engineering
- Sources: public alerts only
- Workflow: discover, normalize, deduplicate, score, explain, tailor, draft, review, approve-to-prepare
- Authentication: simpler MVP auth path first
