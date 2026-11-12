# Spec Delta

## Purpose

Defines invoice approval authority for Managers, Directors, and Admins, including how Admins participate in the existing Director approval limits and high-value approval flow.

## ADDED Requirements

### Requirement: Admins may approve under Director limits
The system SHALL allow Admins to approve invoices under the same amount limits, self-approval restriction, and distinct two-approver rules as Directors.

#### Scenario: Admin approves an invoice within the Director limit
- **WHEN** an Admin approves a pending invoice for 500,000.00 or less and is not its submitter
- **THEN** the invoice is approved

#### Scenario: Admin gives the first approval above the Director limit
- **WHEN** an Admin approves a pending invoice above 500,000.00 as its first approver
- **THEN** the invoice remains pending and records the Admin as the first approver

#### Scenario: Admin completes a high-value approval as a distinct second approver
- **WHEN** an Admin who is different from the first approver approves an invoice above 500,000.00 after a Manager or Director provided the first approval
- **THEN** the invoice is approved and retains the first approver attribution

#### Scenario: Admin cannot approve an invoice they submitted
- **WHEN** an Admin attempts to approve an invoice they submitted
- **THEN** the attempt is denied with an actionable reason and the invoice remains unchanged

### Requirement: Admin approval authority does not grant rejection authority
The system SHALL continue to deny Admins permission to reject invoices.

#### Scenario: Admin attempts to reject an invoice
- **WHEN** an Admin attempts to reject a pending invoice
- **THEN** the attempt is denied and the invoice remains unchanged
