# Security

## Key controls

- No secrets, credentials, or personal data in logs
- Secrets stored in AWS Secrets Manager
- Least-privilege access for all services
- Redaction for sensitive fields in telemetry
- Explicit approval gates before any application preparation that reaches submission state

## Initial MVP posture

- Keep the first release simple and auditable
- Avoid broad automation surfaces that increase risk
- Prefer explicit user review over autonomous action
