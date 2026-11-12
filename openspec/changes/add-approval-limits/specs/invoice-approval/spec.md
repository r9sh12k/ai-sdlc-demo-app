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

### Requirement: Requesters can manage their invoices but cannot decide them
The system SHALL allow a Requester to create, submit, and view invoices, and SHALL deny Requesters permission to approve or reject invoices.

#### Scenario: Requester creates an invoice
- **WHEN** a signed-in Requester creates an invoice
- **THEN** the invoice is created

#### Scenario: Requester submits an invoice
- **WHEN** a signed-in Requester submits an invoice
- **THEN** the invoice is submitted and records the signed-in username as `SubmittedBy`

#### Scenario: Requester views invoices
- **WHEN** a signed-in Requester requests invoice details or the invoice list
- **THEN** the requested invoice information is returned

#### Scenario: Requester cannot approve an invoice
- **WHEN** a Requester attempts to approve an invoice
- **THEN** the attempt is denied and the invoice remains unchanged

#### Scenario: Requester cannot reject an invoice
- **WHEN** a Requester attempts to reject an invoice
- **THEN** the attempt is denied and the invoice remains unchanged

### Requirement: Submitters cannot approve their own invoices
When `SubmittedBy` is present, the system SHALL deny approval by that submitter at either approval stage, regardless of role or invoice amount. It SHALL compare usernames after trimming and ignoring case, and check this rule before applying role and amount limits.

#### Scenario: Submitter cannot provide the first approval
- **WHEN** a Manager who submitted an invoice attempts its first approval within the Manager's amount limit
- **THEN** the attempt is denied with `You submitted this invoice, so someone else must approve it.` and the invoice remains unchanged

#### Scenario: Submitter cannot provide the second approval
- **WHEN** the submitter of an invoice above 500,000.00 attempts its second approval as a Director after a different user provided the first approval
- **THEN** the attempt is denied with `You submitted this invoice, so someone else must approve it.` and the invoice remains unchanged

#### Scenario: Submitter identity matching ignores case and surrounding whitespace
- **WHEN** the username attempting approval matches `SubmittedBy` after trimming and ignoring case
- **THEN** the attempt is denied before role or amount limits are evaluated and the invoice remains unchanged

### Requirement: Self-approval denials are actionable and non-mutating
A self-approval denial SHALL return `You submitted this invoice, so someone else must approve it.`, leave every invoice field unchanged, and SHALL NOT call `Update`. Invoices without `SubmittedBy` SHALL remain unaffected by this rule.

#### Scenario: Invoice without a submitter follows existing approval rules
- **WHEN** an invoice has no `SubmittedBy` and an otherwise eligible approver attempts approval
- **THEN** the self-approval rule does not deny the attempt

### Requirement: High-value approvals require two distinct approvers
For an invoice above 500,000.00, the system SHALL require a Manager or Director first and a distinct Director second. Usernames SHALL be compared after trimming and ignoring case.
The system SHALL retain `FirstApprovedBy` when the second approval completes.

#### Scenario: The same user cannot approve twice with different casing
- **WHEN** the first approver attempts a second approval using the same username with different casing or surrounding whitespace
- **THEN** the attempt is denied and the invoice remains unchanged

#### Scenario: Second approval retains the first approver
- **WHEN** a distinct Director completes a high-value invoice's second approval
- **THEN** the invoice remains approved and retains `FirstApprovedBy` alongside the second approver

### Requirement: Invoice list attributes submissions and approvals
The invoice list SHALL show `Submitted by <name>` for an invoice with a submitter. For an invoice with one recorded approver, it SHALL show `Approved by <name>`; when two approvers are recorded, it SHALL show `Approved by <first> and <second>`.

#### Scenario: Invoice list shows submitter and one approver
- **WHEN** an invoice list includes an invoice with a submitter and one recorded approver
- **THEN** it shows `Submitted by <name>` and `Approved by <name>`

#### Scenario: Invoice list shows both approvers
- **WHEN** an invoice list includes a high-value invoice with two recorded approvers
- **THEN** it shows `Submitted by <name>` and `Approved by <first> and <second>`

### Requirement: Approval denial preserves the invoice and explains the reason
When an approval attempt is denied, the system SHALL provide an actionable reason and leave every invoice field unchanged.

#### Scenario: Admin attempts approval
- **WHEN** an Admin attempts to approve an invoice
- **THEN** the attempt is denied with a clear reason and the invoice remains unchanged

#### Scenario: API reports a denied approval
- **WHEN** an approval attempt is denied through the API
- **THEN** the response has status 403 and a JSON body containing the denial reason in the `error` field
