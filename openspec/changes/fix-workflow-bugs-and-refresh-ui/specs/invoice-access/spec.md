# Spec Delta

## Purpose

Limits invoice operations to organizational scope and gives users clear visibility into ownership, routed approvers, and rejection outcomes.

## ADDED Requirements

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
