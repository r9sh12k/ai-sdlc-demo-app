# Spec Delta

## Purpose

Defines how the web workspace presents invoices, approvals, feedback and available actions so that each signed-in person sees only the actions the server allows and always sees the result of what they did.

## ADDED Requirements

### Requirement: UI offers only the actions the server allows
The workspace SHALL show Submit, Approve and Reject controls only when the list item reports that the signed-in person may take that action. The UI SHALL NOT work out approval permissions itself.

#### Scenario: DM sees a requester's draft
- **WHEN** a DM views a draft created by a requester on the DM's project
- **THEN** no `Submit invoice <id> for approval` button is shown for it

#### Scenario: Requester sees their own draft
- **WHEN** the requester views their own draft
- **THEN** a `Submit invoice <id> for approval` button is shown

### Requirement: Actions cannot be sent twice by repeated clicks
While a sign-in, create, submit or decision request is running, the workspace SHALL disable the control that started it and SHALL send only one request.

#### Scenario: Requester double-clicks Create invoice
- **WHEN** a requester double-clicks `Create invoice` with a valid form
- **THEN** exactly one invoice with that customer name appears in the list

### Requirement: Errors and outcomes are always visible
The workspace SHALL show every load and action error in a visible alert, and SHALL announce each successful create, submit, approve and reject in a status message.

#### Scenario: Project options fail to load
- **WHEN** loading the signed-in person's project options fails
- **THEN** the error appears in a visible alert on the Invoices panel

#### Scenario: Invoice is created
- **WHEN** a requester creates an invoice
- **THEN** a status message reads `Invoice <id> created as a draft.`

#### Scenario: Decision is denied by the server
- **WHEN** the server denies an approval or rejection with a reason
- **THEN** that reason appears in an alert at the top of the Approvals panel

### Requirement: Approvals are grouped by state
The Approvals panel SHALL list pending invoices under a `Pending` heading and invoices the person already decided under a `Decided` heading, each with an empty-state message when it has no items.

#### Scenario: Approver has pending and decided invoices
- **WHEN** an approver opens Approvals with one pending invoice and one invoice they approved
- **THEN** the pending invoice appears in the `Pending` list and the approved invoice in the `Decided` list

### Requirement: Each sign-in starts on the Invoices tab
After sign-in the workspace SHALL select the Invoices tab, whatever tab the previous session had open.

#### Scenario: Approver signs out from Approvals and another user signs in
- **WHEN** a Manager signs out while the Approvals tab is selected and a Director signs in
- **THEN** the Invoices tab is selected and the Invoices panel is shown

### Requirement: Workspace layout is readable and accessible
The workspace SHALL fit its invoice table and approval cards without page-wide horizontal scrolling at 1280px width or more. It SHALL show status as a labelled badge, right-align amounts, show visible keyboard focus, and keep every existing accessible name, column order and dialog.

#### Scenario: Invoice table at desktop width
- **WHEN** the invoice list is shown in a 1280px-wide window
- **THEN** all eleven columns are visible without scrolling the page sideways
