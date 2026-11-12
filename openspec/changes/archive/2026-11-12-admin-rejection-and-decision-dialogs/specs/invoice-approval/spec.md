# Spec Delta

## Purpose

Defines who may reject a pending invoice and the required rejection input, including the Admin role.

## ADDED Requirements

### Requirement: Admins may reject pending invoices
The system SHALL allow Admins to reject pending invoices using the existing rejection workflow.

#### Scenario: Admin rejects a pending invoice with a reason
- **WHEN** an Admin rejects a pending invoice with a non-empty reason
- **THEN** the invoice is rejected and records the Admin and reason

#### Scenario: Admin rejection does not bypass invoice state validation
- **WHEN** an Admin attempts to reject an invoice that is not pending
- **THEN** the attempt is denied and the invoice remains unchanged

### Requirement: Rejection requires a non-empty reason
The system SHALL reject an invoice only when the supplied reason contains non-whitespace characters.

#### Scenario: Rejection has no reason
- **WHEN** a signed-in approver attempts to reject an invoice with an empty or whitespace-only reason
- **THEN** the invoice remains unchanged and no rejection is recorded
