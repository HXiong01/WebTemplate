# Functionality Requirements

Every functionality need to have a test harness to test indicpendetntly and have also logs.

This template enforces that rule as a project convention:

- New functionality must be isolated behind a service, endpoint, or component boundary.
- Each new functionality must get an independent harness case under `tests/WebTemplate.Harness`.
- Each app entry point and feature flow must write useful operational logs to `storage/logs`.
- Launch and test scripts must keep working after new functionality is added.
