# invoice-approval-routing Specification

## Purpose

Defines project-based approval routing, upward escalation, distinct approval steps, and who may decide a routed invoice.

## Requirements

### Requirement: Approval route follows invoice amount and organization roles
The system SHALL route amounts up to 50,000.00 to the project DM, amounts above 50,000.00 through 500,000.00 to the Business Unit head, and amounts above 500,000.00 to the DM followed by a distinct Director (Business Unit head). Admin MAY be a first- or single-step fallback, but SHALL NOT be assigned as the second approver for an invoice above 500,000.00. If no distinct Director is available, submission SHALL fail with `No second approver available`.

#### Scenario: Low-value invoice routes to the DM
- **WHEN** a project with a DM submits an invoice for 50,000.00 or less
- **THEN** the DM is the first routed approver

#### Scenario: Low-value invoice without a DM routes upward
- **WHEN** a project without a DM submits an invoice for 50,000.00 or less
- **THEN** the Business Unit head is the first routed approver, or Admin if the head is unavailable

#### Scenario: Mid-value invoice routes to the Business Unit head
- **WHEN** a project submits an invoice above 50,000.00 and at or below 500,000.00
- **THEN** its Business Unit head is the first routed approver, or Admin if the head is unavailable

#### Scenario: High-value invoice routes to two distinct people
- **WHEN** a project with a DM submits an invoice above 500,000.00
- **THEN** its DM is the first routed approver and its Business Unit head is the second

#### Scenario: High-value invoice without a DM reserves the Director for the second step
- **WHEN** a project without a DM submits an invoice above 500,000.00
- **THEN** Admin is the first routed approver and the Business Unit head is the distinct second approver

#### Scenario: No distinct Director is available for the second step
- **WHEN** an invoice above 500,000.00 has no distinct Director available after the first approver is assigned
- **THEN** no second approver is assigned and submission fails with `No second approver available`

#### Scenario: Duplicate route assignees are not replaced with Admin
- **WHEN** both high-value approval steps would otherwise be assigned to the same person and no distinct Director is available
- **THEN** the second step remains unassigned

#### Scenario: No distinct Director can be assigned
- **WHEN** no distinct Director is available for the second step
- **THEN** the system reports `No second approver available` and leaves the invoice in Draft

### Requirement: Approval routing excludes the requester and lower-ranked people
The system SHALL NOT route an approval step to the requester or to a person at or below the requester's routing rank. It SHALL move that step upward through DM, Business Unit head, and Admin, in that order.

#### Scenario: Requester is the project DM
- **WHEN** the requester is the DM assigned for the first approval step
- **THEN** the first step moves to the Business Unit head or the next higher eligible role

#### Scenario: Requester is the Business Unit head
- **WHEN** the requester is assigned to a Business Unit head step
- **THEN** the step moves to Admin

#### Scenario: DM requester of a high-value invoice
- **WHEN** the project DM requests an invoice above 500,000.00
- **THEN** the first step moves past the DM and the reserved Business Unit head to Admin, and the Business Unit head is the second approver

#### Scenario: Escalation keeps high-value approvers distinct
- **WHEN** upward escalation would assign both high-value steps to one person and no distinct Director is available
- **THEN** the second step remains unassigned

### Requirement: Only the current routed approver may approve
The system SHALL allow approval only by the person currently assigned to the invoice's routed step. Admin SHALL approve an invoice at or below 500,000.00, or the first step of a high-value invoice, only when routed to Admin. Admin SHALL NOT complete a high-value second step.

#### Scenario: Current routed approver approves
- **WHEN** the person assigned to the current step approves a pending invoice
- **THEN** that step is recorded and the invoice advances to its next routed step or Approved status

#### Scenario: Non-routed person attempts approval
- **WHEN** a signed-in person who is not assigned to the current step attempts approval
- **THEN** the attempt is denied with the current required approver identified and the invoice remains unchanged

#### Scenario: Admin cannot complete a high-value second step
- **WHEN** Admin is assigned as a second approver for an invoice above 500,000.00
- **THEN** the approval is denied because the second approver must be a Director

### Requirement: Decisions store trimmed attribution and reasons
When an invoice is approved or rejected, the system SHALL store the deciding username and the rejection reason without leading or trailing spaces.

#### Scenario: Approver name has surrounding spaces
- **WHEN** the routed approver approves with the username ` demo.manager `
- **THEN** the invoice records `demo.manager` as the decider

#### Scenario: Rejection reason has surrounding spaces
- **WHEN** an authorized person rejects an invoice with the reason `  Duplicate invoice.  `
- **THEN** the invoice records `Duplicate invoice.` as the rejection reason
- **AND** the rejection attribution reads `username (username@example.com): Duplicate invoice.`

### Requirement: Routed approvers may reject and Admin may reject any pending invoice
The system SHALL allow the current routed approver or Admin to reject a pending invoice when a non-empty reason is provided. Rejection SHALL record the person and reason without bypassing invoice-state validation.

#### Scenario: Current routed approver rejects with a reason
- **WHEN** the current routed approver rejects a pending invoice with a non-empty reason
- **THEN** the invoice is Rejected and records that person and reason

#### Scenario: Admin rejects an invoice outside their route
- **WHEN** Admin rejects any pending invoice with a non-empty reason
- **THEN** the invoice is Rejected and records Admin and the reason

#### Scenario: Rejection without a reason is denied
- **WHEN** a routed approver or Admin rejects an invoice with an empty or whitespace-only reason
- **THEN** the attempt is denied and the invoice remains unchanged
