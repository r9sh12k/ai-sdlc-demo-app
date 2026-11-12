# invoice-access Specification

## Purpose

Limits invoice operations to organizational scope and gives users clear visibility into ownership, routed approvers, and rejection outcomes.

## Requirements

### Requirement: Invoice creation requires project allocation
The system SHALL require a project for invoice creation and SHALL allow creation only when the signed-in person is allocated to that project. A project's DM SHALL count as allocated.

#### Scenario: Allocated Requester creates an invoice for a project
- **WHEN** a Requester creates an invoice for a project to which they are allocated
- **THEN** the invoice records that project and the signed-in person as its requester

#### Scenario: User creates an invoice for an unallocated project
- **WHEN** a signed-in person creates an invoice for a project to which they are not allocated
- **THEN** the system denies creation and does not add an invoice

### Requirement: Invoice list is limited to the person's scope
The system SHALL return only invoices in the signed-in person's invoice scope: Requesters' own invoices, DMs' projects, Business Unit heads' Business Unit, or all invoices for Admin.

#### Scenario: Requester views invoices
- **WHEN** a Requester requests the invoice list
- **THEN** only invoices they requested are returned

#### Scenario: DM views invoices
- **WHEN** a DM requests the invoice list
- **THEN** invoices for that DM's projects are returned

#### Scenario: Business Unit head views invoices
- **WHEN** a Business Unit head requests the invoice list
- **THEN** invoices for that person's Business Unit are returned

#### Scenario: Admin views invoices
- **WHEN** Admin requests the invoice list
- **THEN** all invoices are returned

### Requirement: Approval list combines scoped pending invoices with the person's decisions
The system SHALL show DMs' pending invoices for their projects, Business Unit heads' pending invoices for their Business Unit, and all pending invoices to Admin. It SHALL also include invoices on which the signed-in person has recorded a decision.

#### Scenario: Approver views pending invoices in scope
- **WHEN** a DM or Business Unit head opens the approval list
- **THEN** pending invoices in their respective project or Business Unit scope are shown with the current `Waiting on` person

#### Scenario: Admin views pending invoices
- **WHEN** Admin opens the approval list
- **THEN** all pending invoices are shown with the current `Waiting on` person

#### Scenario: Approver sees an invoice they already decided
- **WHEN** an invoice has a decision recorded by the signed-in approver
- **THEN** that invoice appears in the approver's list even if it is no longer pending or in their current scope

### Requirement: Invoice rows show project, requester, routed approvers, and outcome
The invoice list SHALL show Project, Requested By, Approval 1, Approval 2, Waiting on, Status, and Rejection reason while retaining existing invoice identity and amount details. Person values SHALL use `username (email)`; the Approval columns SHALL show routed assignees before they act.

#### Scenario: Invoice row shows the approval route
- **WHEN** an invoice appears in a list
- **THEN** it shows its project, requester, and routed Approval 1 person
- **AND** Approval 2 shows its routed person above 500,000.00 and `Not applicable` at or below 500,000.00

#### Scenario: Invoice row shows the person currently expected to act
- **WHEN** an invoice is pending approval
- **THEN** `Waiting on` shows the current routed approver as `username (email)`

#### Scenario: Invoice row shows rejection attribution
- **WHEN** an invoice is Rejected
- **THEN** Rejection reason shows `username (email): reason`

#### Scenario: Non-rejected invoice has no rejection reason
- **WHEN** an invoice is not Rejected
- **THEN** its Rejection reason field is empty

### Requirement: Requesters cannot access the approval queue
The system SHALL NOT expose the approval queue to Requesters; their invoices remain available through their scoped invoice list.

#### Scenario: Requester opens the approval section
- **WHEN** a Requester opens the application
- **THEN** no approval queue items or decision actions are available

### Requirement: Only the draft's requester may submit it
The system SHALL allow a draft to be submitted only by the requester recorded on it, compared after trimming and ignoring case. A denied submit SHALL return a reason the user can act on and SHALL leave the invoice unchanged.

#### Scenario: Requester submits their own draft
- **WHEN** the requester recorded on a draft submits it
- **THEN** the invoice moves to Pending Approval with its routed approvers recorded

#### Scenario: Another user submits someone else's draft
- **WHEN** a signed-in person who is not the draft's requester submits it
- **THEN** the system denies the submit with `Only the requester who created this draft can submit it.`
- **AND** the invoice's status, requester and routed approvers are unchanged and nothing is saved

### Requirement: Sign-in matches usernames without regard to case or surrounding spaces
The system SHALL accept a demo username at sign-in after trimming it and ignoring letter case. The session SHALL carry the canonical username so later comparisons and displays use one identity.

#### Scenario: User signs in with different letter case
- **WHEN** a person signs in as ` Demo.Manager ` with the correct password
- **THEN** sign-in succeeds and the session username is `demo.manager`

#### Scenario: Wrong password is still refused
- **WHEN** a person signs in with a known username and a wrong password
- **THEN** sign-in fails with `Invalid username or password.`

### Requirement: List items report the signed-in person's available actions
Each invoice in the invoice list and the approval list SHALL state whether the signed-in person may submit, approve or reject it. These flags SHALL use the same rules as the submit, approve and reject actions.

#### Scenario: Requester views their draft
- **WHEN** the requester of a draft views the invoice list
- **THEN** that draft reports that they may submit it and may not approve or reject it

#### Scenario: DM views a requester's draft in scope
- **WHEN** a DM views a draft created by a requester on the DM's project
- **THEN** that draft reports that the DM may not submit it

#### Scenario: Routed approver views a pending invoice
- **WHEN** the current routed approver views a pending invoice
- **THEN** it reports that they may approve and reject it

#### Scenario: Admin views a pending invoice routed to someone else
- **WHEN** Admin views a pending invoice routed to a DM
- **THEN** it reports that Admin may reject it and may not approve it

### Requirement: Approval list shows the first approver's identity
The approval list SHALL show a recorded first approver as `username (username@example.com)`.

#### Scenario: High-value invoice after its first approval
- **WHEN** a high-value invoice has its first approval and appears in the approval list
- **THEN** the item shows `First approved by demo.manager (demo.manager@example.com)`
