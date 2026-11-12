# Spec Delta

## Purpose

Provides clear, temporary feedback when a signed-in user attempts an action their role is not permitted to perform.

## ADDED Requirements

### Requirement: Permission denials are shown in a dismissible modal
When an action is denied because of the user's role, the system SHALL present the existing permission-denied message in a modal that the user can dismiss.

#### Scenario: Role-restricted action is denied
- **WHEN** the server denies an action with `Your account does not have permission to perform this action.`
- **THEN** the UI displays that message in a modal dialog

#### Scenario: User dismisses the permission modal
- **WHEN** the user dismisses the permission-denied modal
- **THEN** the modal closes and the message is no longer visible

### Requirement: Permission-denied feedback does not persist across sessions
The system SHALL clear any displayed permission-denied feedback when the user session changes or ends.

#### Scenario: User signs out while permission feedback is displayed
- **WHEN** the user signs out while a permission-denied modal is open
- **THEN** the modal closes and its message is not shown to the next signed-in user
