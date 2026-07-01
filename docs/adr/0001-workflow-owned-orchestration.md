# ADR 0001: Workflow-owned orchestration

## Status

Accepted

## Context

The system must coordinate multiple AI-assisted tasks such as job discovery, scoring, resume tailoring, and application drafting. A fully autonomous model-driven approach would be difficult to audit and could bypass human control.

## Decision

The workflow engine will own the lifecycle and state transitions for each job and application draft. Agents will act as constrained workers with structured input and output contracts.

## Consequences

- Better auditability and observability
- Clearer separation of responsibilities
- Safer human review and approval controls
