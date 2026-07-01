# ADR 0002: Human-in-the-loop approval

## Status

Accepted

## Context

The system will prepare application materials, but the user requires explicit approval before any application is submitted.

## Decision

The platform will implement two distinct approval gates:

1. Approval to prepare application materials
2. Approval to submit the application

Submission will remain blocked until the user explicitly approves it.

## Consequences

- Stronger user control and trust
- Clearer audit trail for each decision
- Lower risk of unintended application submission
