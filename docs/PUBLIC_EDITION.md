# Public portfolio edition

[Back to the project](../README.md)

This repository is a deliberately small, fresh source snapshot of ArchiveSystem. It presents the architecture and application interface for technical review and collaboration.

## Included

- Application source, backend project files, database migrations and test source.
- Frontend source, package lockfile, required WOFF2 fonts and their upstream license.
- Empty/sanitized service settings and clearly labeled environment examples.
- A portable .NET solution and localhost development Compose example.
- English/Persian introduction, architecture and engineering notes.
- SVG diagrams and compressed WebP captures using synthetic demo data.

## Excluded

- Contracts, delivery agreements and internal customer documents.
- Operational uploads, identity-document images, avatars and exported records.
- Real environment files, passwords, connection strings and deployment-specific settings.
- Previous Git history and remote addresses.
- Dependency installations, caches, compiled binaries, test results and debug reports.
- Duplicate ZIP/RAR archives and OCR model binaries.

Generated sample records are part of the original frontend's mock adapters; their phone fields use non-dialable placeholders in this edition. The interface displays an explicit portfolio/demo label. A neutral wordmark replaces deployment branding.

## Repository size

The repository tracks source and small documentation assets rather than installed dependencies or build output. Font assets use WOFF2. Screenshots use WebP. OCR models must be provisioned locally according to their publishers' terms, avoiding large opaque binaries in Git history.

The initial publication is a single fresh commit. The original working directory, local changes and remotes remain separate from this edition.

## Configuration and reuse

Service settings intentionally contain empty credentials. Copy the environment examples locally and supply new values for a real backend setup. The frontend showcase is separate from real service authentication and does not contact the backend.

This publication does not replace a license/provenance review for reuse. Existing third-party attributions remain intact; discuss application reuse and commercial collaboration with the repository owner.
