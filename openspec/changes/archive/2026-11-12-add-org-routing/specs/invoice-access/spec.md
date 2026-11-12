# Spec Delta

## Purpose

Limits invoice operations to organizational scope and gives users clear visibility into ownership, routed approvers, and rejection outcomes.

## ADDED Requirements

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
