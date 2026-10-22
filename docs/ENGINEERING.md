# Engineering walkthrough

[Back to the project](../README.md)

## Start with a use case

Follow a case creation request through the [case command handlers](../src/ArchiveSystem.Application/Features/Cases/Commands/), the [domain case model](../src/ArchiveSystem.Domain/Entities/Case.cs), and infrastructure persistence. Compare the API's validation/error translation with the frontend's request mapping. This shows how the project separates user interaction from business operations and database details.

## Flexible business forms

The [dynamic-field features](../src/ArchiveSystem.Application/Features/DynamicFields/) and domain definitions support templates, groups, options and typed values. This is useful when classifications require different metadata rather than a single fixed form. On the frontend, [`DynamicFieldsAdminPage`](../src/ArchiveSystem.Frontend/src/pages/DynamicFieldsAdminPage.tsx) provides the administration view.

## Long-running document work

Inspect [`OcrWorker`](../src/ArchiveSystem.Worker.Ocr/Workers/OcrWorker.cs) and [`IndexerWorker`](../src/ArchiveSystem.Worker.Indexer/Workers/IndexerWorker.cs). Their loops, batch sizes and backfill paths keep processing outside the API request. Review the event consumers alongside these loops to understand which paths are enabled for a deployment. Polling and queue consumers introduce concurrency/idempotency questions that need realistic load and failure tests.

## A business application interface

The frontend combines React Query for server data, Zustand for UI/session state, Material UI for tables and forms, and explicit RTL styling. [`ArchiveLayout`](../src/ArchiveSystem.Frontend/src/components/app/ArchiveLayout.tsx) coordinates navigation, permissions and the classification tree. [`ArchiveWorkspacePage`](../src/ArchiveSystem.Frontend/src/pages/ArchiveWorkspacePage.tsx) connects case lists and details through URL state.

The UI includes older compatibility paths and mock adapters. The public showcase intentionally reuses those fixtures rather than connecting to operational data. It demonstrates interface behavior, not backend performance.

## Infrastructure choices

Storage is registered through configuration in [`DependencyInjection`](../src/ArchiveSystem.Infrastructure/DependencyInjection.cs). Document bytes, metadata and search indexes can evolve independently, but operating SQL Server, a broker, object storage and search increases deployment complexity. An organization can choose required workers and adapters rather than assuming every optional integration is available.

## Validation and current limits

Test projects cover different parts of the system; the [verification report](VALIDATION.md) records the current execution results. Unit/build checks do not replace a deployment test with SQL, RabbitMQ, MinIO, OpenSearch, OCR models and physical scanners.

Before a production deployment, priorities include dependency review, secrets management, backup/restore drills, upload/processing failure recovery, representative OCR evaluation, and permission regression tests with realistic role/classification combinations. The public source includes external connector code, but excludes customer endpoint settings and database connections.

## Collaboration opportunities

This project is a useful starting point for discussing:

- Internal document/case workflows and role-based business applications.
- Persian/RTL React interfaces and complex metadata forms.
- .NET API design, EF Core persistence and application boundaries.
- Background document processing, search and storage integration.
- Scanner/desktop bridges and on-premises deployment requirements.

For project-specific work, [contact the repository owner through GitHub](https://github.com/Ali771025) and discuss the intended scope, deployment constraints and reuse permissions.
