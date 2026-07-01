# Data Model

## Core entities

- UserProfile
- JobPosting
- JobMatchResult
- ResumeProfile
- TailoredResume
- ApplicationDraft
- ApprovalDecision
- AuditEvent
- SourceFeed

## Notes

The model should be designed to support:
- durable workflow tracking
- auditability of every decision
- separation of draft artifacts from approved artifacts
- human review states and approval history
