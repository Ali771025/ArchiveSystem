# Verification record

This page distinguishes checks performed during preparation from runtime guarantees. Date: 2026-10-22.

| Check | Recorded outcome |
| --- | --- |
| Frontend dependency installation | `npm ci` completed from the supplied lockfile. |
| Frontend type checking and production build | `npm run build` completed, including TypeScript checking and a Vite production build. Vite reported a large-chunk warning. |
| .NET dependency restore | Restore completed for the portable solution. Package vulnerability advisories were reported for the existing ImageSharp dependency. |
| Initial .NET solution build | Application/API/worker compilation progressed, but the test-project build reported a missing xUnit namespace import. A global xUnit import was added to the Infrastructure test project; the final solution build was not rerun. |
| Test execution | Initial runs were incomplete and included failures. Browser-test environment/cleanup configuration was added. Further test runs were stopped at the owner's request; no passing full-suite result is claimed. |
| Lint | No completed clean lint result is claimed. |
| Publication review | Private configuration was replaced, operational files and history were excluded, and publication contents were reviewed for secrets. Gitleaks found no leaks in the staged publication. |
| UI images | Captured from the actual React interface using synthetic showcase fixtures. They are UI evidence only. |
| Deployment / OCR / scanner | No end-to-end service deployment, OCR accuracy benchmark, or physical scanner validation is claimed. |

## Reproduce checks when needed

```bash
dotnet restore src/ArchiveSystem.sln
dotnet build src/ArchiveSystem.sln -c Release --no-restore
dotnet test src/ArchiveSystem.sln -c Release --no-build
cd src/ArchiveSystem.Frontend
npm ci
npm run build
npm test -- --run
npm run lint
```

The source edition includes test projects for review and future validation. These commands are provided for maintainers; publication does not mean all tests have passed. Package advisories, compiler warnings, integration prerequisites and older compatibility paths should be reviewed before production use.
