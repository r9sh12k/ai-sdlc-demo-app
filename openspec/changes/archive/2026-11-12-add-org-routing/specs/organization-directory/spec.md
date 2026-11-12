# Spec Delta

## Purpose

Defines the business-unit, account, project, and allocation structure used to identify invoice ownership and eligible approvers.

## ADDED Requirements

### Requirement: Organization hierarchy identifies business units, accounts, and projects
The system SHALL associate each account with one Business Unit and each project with one account, forming a Business Unit > Account > Project hierarchy. Each Business Unit SHALL have a head, and each project MAY have one Delivery Manager (DM).

#### Scenario: Demo organization contains the requested hierarchy
- **WHEN** the demo organization is initialized
- **THEN** BU-DIGITAL is headed by `demo.director` and contains ACC-ACME with PRJ-PORTAL (DM `demo.manager`) and PRJ-MOBILE (DM `demo.manager2`)
- **AND** BU-ENTERPRISE is headed by `demo.director2` and contains ACC-GLOBEX with PRJ-ERP and no DM

### Requirement: Project allocations identify a person's project role
The system SHALL associate a person with a project allocation and role. A project's DM SHALL count as allocated to that project.

#### Scenario: Demo Requesters are allocated to their projects
- **WHEN** the demo organization is initialized
- **THEN** `demo.requester` is a Requester on PRJ-PORTAL and PRJ-MOBILE, and `demo.requester2` is a Requester on PRJ-ERP

#### Scenario: Delivery Manager is treated as allocated
- **WHEN** the system checks whether a project's DM is allocated to that project
- **THEN** the DM is treated as a project member even without a separate allocation record

### Requirement: Demo identities have stable email addresses
The system SHALL provide each demo identity an email address formed as `<username>@example.com`.

#### Scenario: Demo organization exposes email identities
- **WHEN** the system returns a demo person for display
- **THEN** the person's email is their username followed by `@example.com`
