# Proposal

## Why

Admins currently cannot approve invoices even when they should participate in the established Director approval workflow. When an approval action is forbidden, the UI leaves the permission message embedded in the page, where it can remain visible across login changes instead of behaving like a temporary interruption.

## What Changes

- Allow Admins to approve invoices using the existing Director limits, including the distinct second-approver requirement above 500,000.00.
- Keep Admins unable to reject invoices; this request changes approval authority only.
- Present the existing permission-denied message in a dismissible modal and clear it when the modal is dismissed or the session changes.

## Capabilities

### New Capabilities
- `invoice-approval`: Admin approval authority under Director limits and existing approval rules.
- `permission-feedback`: Modal presentation and dismissal of permission-denied feedback.

### Modified Capabilities
- None. The repository has no main OpenSpec capability specs yet; these behaviors will be captured as new capabilities.

## Impact

- Domain approval-role evaluation and API authorization for the approval action.
- Static UI markup and JavaScript handling of 403 permission-denied responses.
- Domain/API tests and focused browser or UI tests.
