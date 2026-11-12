# decision-confirmations Specification

## Purpose

Provides clear confirmation and validation feedback before users decide the outcome of a pending invoice.

## Requirements

### Requirement: Approval requires confirmation
The system SHALL ask the user to confirm before submitting an approval decision.

#### Scenario: User confirms approval
- **WHEN** the user confirms approval of a pending invoice
- **THEN** the approval is submitted

#### Scenario: User cancels approval
- **WHEN** the user cancels the approval confirmation
- **THEN** no approval is submitted and the invoice remains pending

### Requirement: Rejection requires confirmation
The system SHALL ask the user to confirm before submitting a rejection decision.

#### Scenario: User confirms rejection
- **WHEN** the user confirms rejection of a pending invoice with a non-empty reason
- **THEN** the rejection is submitted with that reason

#### Scenario: User cancels rejection
- **WHEN** the user cancels the rejection confirmation
- **THEN** no rejection is submitted and the invoice remains pending

### Requirement: Missing rejection reason is reported in a dialog
The system SHALL show an error dialog and not submit a rejection when the reason is empty or whitespace-only.

#### Scenario: User rejects without a reason
- **WHEN** the user attempts to reject a pending invoice without a non-whitespace reason
- **THEN** an error dialog explains that a reason is required and the invoice remains pending
