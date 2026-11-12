# Spec Delta

## Purpose

Defines amount- and role-based invoice approval, including distinct two-person approval for high-value invoices and clear, non-mutating denial behavior.

## ADDED Requirements

### Requirement: Approval authority is limited by role and amount
The system SHALL allow Managers to approve invoices up to 50,000.00 and Directors to approve invoices up to 500,000.00, inclusive; only a Director may complete approval above 500,000.00.

#### Scenario: Manager approves 50,000.00
- **WHEN** a Manager approves an invoice for 50,000.00
- **THEN** the invoice is approved

#### Scenario: Manager cannot approve 50,000.01
- **WHEN** a Manager attempts to approve an invoice for 50,000.01
- **THEN** the attempt is denied with a clear reason and the invoice remains unchanged

#### Scenario: Director approves 500,000.00
- **WHEN** a Director approves an invoice for 500,000.00
- **THEN** the invoice is approved

#### Scenario: 500,000.01 requires two approvals
- **WHEN** a Manager or Director approves an invoice for 500,000.01 as the first approver
- **THEN** the invoice remains PendingApproval and records that approver as the first approver

#### Scenario: Manager is the second approver
- **WHEN** a Manager attempts to complete approval of an invoice for 500,000.01 after its first approval
- **THEN** the attempt is denied with a clear reason and the invoice remains unchanged

### Requirement: High-value approvals require two distinct approvers
For an invoice above 500,000.00, the system SHALL require a Manager or Director first and a distinct Director second. Usernames SHALL be compared after trimming and ignoring case.

#### Scenario: The same user cannot approve twice with different casing
- **WHEN** the first approver attempts a second approval using the same username with different casing or surrounding whitespace
- **THEN** the attempt is denied and the invoice remains unchanged

### Requirement: Approval denial preserves the invoice and explains the reason
When an approval attempt is denied, the system SHALL provide an actionable reason and leave every invoice field unchanged.

#### Scenario: Admin attempts approval
- **WHEN** an Admin attempts to approve an invoice
- **THEN** the attempt is denied with a clear reason and the invoice remains unchanged

#### Scenario: API reports a denied approval
- **WHEN** an approval attempt is denied through the API
- **THEN** the response has status 403 and a JSON body containing the denial reason in the `error` field
