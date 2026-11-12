# Spec Delta

## Purpose

Defines project-based approval routing, upward escalation, distinct approval steps, and who may decide a routed invoice.

## ADDED Requirements

### Requirement: Decisions store trimmed attribution and reasons
When an invoice is approved or rejected, the system SHALL store the deciding username and the rejection reason without leading or trailing spaces.

#### Scenario: Approver name has surrounding spaces
- **WHEN** the routed approver approves with the username ` demo.manager `
- **THEN** the invoice records `demo.manager` as the decider

#### Scenario: Rejection reason has surrounding spaces
- **WHEN** an authorized person rejects an invoice with the reason `  Duplicate invoice.  `
- **THEN** the invoice records `Duplicate invoice.` as the rejection reason
- **AND** the rejection attribution reads `username (username@example.com): Duplicate invoice.`
