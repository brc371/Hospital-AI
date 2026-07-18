## PIONEER FEATURE: Note version diff view

Implements the "diff view between note versions" pioneer feature suggested in the Challenge
Description, letting a provider see exactly what changed between two saved versions instead of
having to compare two full notes manually.

- `Services/NoteVersionDiffService.cs` (`INoteVersionDiffService`) - computes a classic
  line-based diff between two blocks of text using a longest-common-subsequence (LCS) dynamic
  programming table, so unchanged lines are matched correctly even when lines were inserted or
  removed elsewhere (not just a naive position-by-position comparison). Returns a
  `List<DiffLine>`, each tagged `Added`, `Removed`, or `Unchanged`. Registered as scoped in
  `Program.cs` (no external dependencies/dependencies on the `DbContext`, so it's a small,
  pure/testable service, deliberately kept simple and interview-explainable).
- `Pages/Encounters/CompareVersions.cshtml(.cs)` (new, route `/Encounters/CompareVersions`) -
  takes two note version IDs (`fromId`/`toId` query params), loads both via the existing
  `IEncounterService.GetNoteVersionAsync` (same owning-provider-or-admin access check used by
  `ViewVersion`), always orders them chronologically (older -> newer) regardless of which order
  the IDs were passed in, and computes a separate diff for each of the four SOAP sections
  (Subjective/Objective/Assessment/Plan). The view renders each section's lines with
  added/removed lines highlighted (green/red background, strikethrough for removed) and
  unchanged lines shown plainly.
- `Pages/Encounters/Workspace.cshtml` - the "Saved note versions" table now shows a checkbox per
  row (only when there are 2+ versions to compare) and a "Compare selected versions" button that
  stays disabled until exactly two checkboxes are checked; clicking it navigates to
  `/Encounters/CompareVersions?fromId={id}&toId={id}`.
- No schema/migration changes were needed - this is a pure read/derived-data feature built
  entirely on top of the existing immutable `NoteVersion` records.

## POST-STEP-13 FIX: Self-deactivation guard + clickable note version history

Two gaps surfaced by manual testing on the live Azure deployment, both cross-checked against
`Challenge Description.md` before implementing:

**1. Admin could deactivate their own account.** Since `RoleResolutionService` treats a
deactivated provider as unrecognized (redirects to `/AccessDenied`), an admin deactivating
themselves would be an immediate, unrecoverable self-lockout (short of a direct DB edit). The
spec doesn't call this out explicitly, but it's an obvious defensive fix:
- `Pages/Admin/Providers.cshtml.cs` - added `CurrentAdminId` and `ToggleError` properties.
  `OnPostToggleActiveAsync` now blocks the request when `!isActive && id == provider.Id`,
  setting `ToggleError` instead of calling `SetActiveAsync`.
- `Pages/Admin/Providers.cshtml` - shows a `ToggleError` alert banner; the admin's own active
  row now shows a "This is you" label instead of a "Deactivate" button.

**2. Saved note version history wasn't openable.** The spec explicitly requires: "Providers
can view the full version history of any note, including who saved each version and at what
time." The `Workspace.cshtml` table already showed version number/saved-by/saved-at, but rows
weren't clickable - a provider had no way to actually open and read an old version's content.
- `Services/IEncounterService.cs` / `EncounterService.cs` - added
  `GetNoteVersionAsync(Guid versionId, Guid providerId, bool isAdmin)`, which loads a single
  `NoteVersion` (with `SavedByProvider` and `Encounter.Patient`), scoped to the owning provider
  unless the caller is an Admin.
- `Pages/Encounters/ViewVersion.cshtml.cs` / `.cshtml` (new) - a read-only page showing the
  saved Subjective/Objective/Assessment/Plan text plus who saved it and when, with a clear
  "read-only, versions are immutable" notice and a link back to the encounter workspace.
- `Pages/Encounters/Workspace.cshtml` - each row in the "Saved note versions" table now has a
  "View" link to `./ViewVersion?id={version.Id}`.

No schema/migration changes were needed for either fix. Build verified successful.

## STEP 12 COMPLETE: Non-happy-path scenarios (2 of 2)

Two substantive non-happy-path scenarios are implemented and demonstrable:

**Scenario 1 - Transcript with no clinically meaningful content** (built during Step 7/9):
`NoteGenerationService`'s system prompt asks the AI to generate a SOAP note from the given
transcript; when the transcript is empty or contains nothing clinically relevant, the model
responds with an honest "insufficient information to determine a diagnosis" note (verified
with an empty-transcript test) rather than hallucinating vitals, exam findings, or a fake
diagnosis. No special-case code was needed for this - it emerges from clearly telling the
model to only report what's actually present in the transcript.

**Scenario 2 - Admin deactivates a provider while they have a draft open**:
- `RoleResolutionService.ResolveByEmailAsync` (existing, Step 5) already treats a deactivated
  provider the same as an unrecognized user - it returns `null`, and every page handler
  (`Workspace`, `Encounters/Index`, etc.) redirects unrecognized users to `/AccessDenied`.
  That was sufficient for full page loads/navigations, but not for the client-side autosave
  `fetch` calls introduced in Step 11: `fetch` silently follows a redirect to the HTML
  `/AccessDenied` page (status 200), so the autosave script would have reported a false
  "All changes saved" even though nothing was actually persisted after deactivation.
- `Pages/Encounters/Workspace.cshtml.cs` - `OnPostSaveDraftAsync` now checks
  `IsAjaxRequest()` (a request carrying the `X-Requested-With: XMLHttpRequest` header) and
  returns a plain `401 Unauthorized` status instead of a redirect for that case, while normal
  browser form submissions (the "Save draft"/"Save note" buttons) are unaffected and still
  redirect to `/AccessDenied` as before.
- `Pages/Encounters/Workspace.cshtml` - the JS `saveDraftToServer()` helper now sends that
  header and throws a distinct `DeactivatedAccountError` when it sees a 401 response. Both the
  debounced autosave loop and the "Generate note" pre-save call catch this specific error type
  and call a new `handleAccountDeactivated()` function, which:
  - Shows a persistent, clearly worded `#deactivatedBanner` alert explaining the account was
	deactivated and that work up to the last successful save is preserved.
  - Disables both textareas and the "Generate note" button so no further edits/API calls are
	attempted.
  - Permanently stops the autosave retry loop (rather than silently retrying forever or
	thrashing the server with failing requests).
- `Controllers/NoteGenerationController.cs` - the SSE `generate-note` endpoint already returned
  `403 Forbidden` for a deactivated provider (pre-existing check); confirmed this remains
  correct and is the first line of defense if a deactivation happens between page load and
  clicking "Generate note" (the pre-generation save call now also independently detects and
  surfaces deactivation before the EventSource connection is even opened).
- Defines the reasonable behavior chosen for this scenario: **preserve, don't discard** - the
  provider's last successfully saved draft/note-versions remain fully intact in the database
  (nothing is deleted or rolled back on deactivation), but no further writes are accepted once
  deactivated, and the UI makes this state unambiguous rather than failing silently or
  crashing.

## STEP 11 COMPLETE: Session persistence / draft autosave across devices


- `Pages/Encounters/Workspace.cshtml` - the transcript and draft note textareas already
  persisted to the database via the existing `SaveDraft` handler (from Step 6), but previously
  only when the provider explicitly clicked "Save draft". Step 11 adds true autosave:
  - A debounced (1.5s after the last keystroke) `input` listener on both textareas calls a new
	shared `saveDraftToServer()` function, which POSTs to the existing `?handler=SaveDraft`
	page handler via `fetch` (no full page reload/navigation), reusing the anti-forgery token
	already rendered in the form.
  - A small `#autosaveStatus` indicator next to the action buttons shows "Unsaved changes...",
	"All changes saved.", or an error message, giving the provider visible confidence that
	their work is being persisted continuously (this is what makes "restore from the database
	after a refresh/browser close/different-device login" actually reliable, since the DB is
	now updated seconds after typing rather than only on an explicit save).
  - `insertIcd10CodeIntoAssessment` (Step 9) and the "Generate note" SSE completion handler
	(Step 7) now also call the same autosave scheduling function, since those flows edit
	`DraftNoteText` programmatically without firing a native `input` event.
  - A `beforeunload` listener does a best-effort final save via `navigator.sendBeacon` if a
	debounced autosave was still pending when the tab was closed/navigated away from, since a
	normal `fetch` call is not guaranteed to complete once the page starts unloading.
  - "Generate note" was already refactored (in a prior fix) to save the transcript via the same
	handler before streaming starts, so generation always operates on the latest typed text
	even if autosave hasn't fired yet.
  - No new columns/migrations were needed - `Encounter.TranscriptText`/`DraftNoteText` and the
	existing `OnGetAsync` reload were already sufficient to restore a draft on refresh or on a
	different device/browser once autosave keeps the database current; this step's work was
	entirely front-end (making the client aggressively persist rather than requiring a manual
	save).

## STEP 10 COMPLETE: Admin dashboard (provider roster, note templates, all-encounters view)


- `Services/ITemplateService.cs` / `TemplateService.cs` - CRUD for `NoteTemplate` rows
  (`GetAllAsync`, `GetActiveAsync` for the provider-facing picker, `GetByIdAsync`,
  `CreateAsync`, `UpdateAsync`, `DeleteAsync`). `UpdateAsync` always stamps
  `UpdatedAtUtc`, but the real "live update" behavior comes from `NoteGenerationService`
  re-reading the template fresh from the database on every generation call (no in-memory
  caching anywhere) - so an admin's edit takes effect on the very next "Generate note" click
  a provider makes, with no polling or page refresh required.
- `Services/IProviderManagementService.cs` / `ProviderManagementService.cs` - roster
  management: `GetAllAsync`, `AddAsync(name, email, role)`, `SetActiveAsync(id, isActive)`.
  Deactivating a provider only flips `IsActive`; it never deletes the account or its historical
  encounters/note versions, since `RoleResolutionService.ResolveByEmailAsync` already treats
  inactive providers as unauthenticated (existing Step 5 behavior) - Step 10 just adds the
  admin UI on top of that existing enforcement.
- `Pages/Admin/Index.cshtml(.cs)` - all-encounters view across every provider, filterable by
  provider (dropdown) and date range (`FromDate`/`ToDate`, matched against `CreatedAtUtc`).
  Reuses the existing `IEncounterService.GetAllEncountersAsync()` from Step 6 and filters
  in-memory (dataset is small/demo-scale; a real production system would push filters into
  the EF query, called out as a known simplification).
- `Pages/Admin/Providers.cshtml(.cs)` - lists all providers/admins, an "Add provider" form
  (validates required fields + duplicate email before insert), and a per-row
  Deactivate/Reactivate toggle button.
- `Pages/Admin/Templates.cshtml(.cs)` - lists all templates, an inline create/edit form (edit
  mode driven by an `?editId=` query param), and a Delete button per row (with a client-side
  confirm). Deleting a template sets `Encounter.NoteTemplateId` to `null` via the existing
  `SetNull` delete behavior configured in `ClinicalScribeDbContext`, so past encounters keep
  their history.
- All three Admin pages independently re-check `Role == ProviderRole.Admin` via
  `IRoleResolutionService` and redirect to `/AccessDenied` otherwise - consistent with the
  existing manual role-check pattern used by `Encounters/Index` and `Workspace` rather than
  introducing a new ASP.NET Core authorization policy.
- `Pages/Encounters/Index.cshtml(.cs)` - providers now pick an (optional) active `NoteTemplate`
  from a dropdown on the "Start a new encounter" form; the selection is passed to
  `IEncounterService.StartEncounterAsync(..., noteTemplateId)` and persisted directly on the
  new `Encounter.NoteTemplateId` column (already present in the schema since Step 6/`Encounter`
  model - no new migration was required).
- `Services/NoteGenerationService.cs` - `GenerateNoteStreamAsync` now includes
  `encounter.NoteTemplate` in its query and, if an active template is set, appends its
  `PromptText` to the system prompt as "Additional instructions for this encounter type" -
  this is what makes the AI visibly behave differently per template as required.
- `Pages/Shared/_Layout.cshtml` - added top nav links ("Encounters", and "Admin Dashboard" only
  for signed-in Admins) using the existing `Context.Items["CurrentProvider"]` middleware value.
- `Program.cs` - registered `ITemplateService`/`TemplateService` and
  `IProviderManagementService`/`ProviderManagementService` in DI (scoped, matching the other
  DbContext-dependent services).

## STEP 9 COMPLETE: ICD-10 code search widget


- `Data/Icd10CodeSeedData.cs` - ~220 real, curated ICD-10-CM codes spanning major categories
  (cardiovascular, endocrine, respiratory, musculoskeletal, mental health, infectious disease,
  injuries, preventive/screening codes, etc.) as a static `(Code, Description)` list embedded
  directly in the app - no external ICD-10 API is called at runtime.
- `Data/DbSeeder.cs` - new `SeedIcd10CodesAsync(context)`, idempotent (no-op if `Icd10Codes`
  already has rows), called from `Program.cs` startup alongside the existing provider seeding.
- `Services/IIcd10SearchService.cs` / `Icd10SearchService.cs` - "semantic search" here means
  explainable, in-memory relevance-ranked text search (not true vector/embedding search, since
  no embeddings deployment exists in `AISettings` and it would need a new Azure resource):
  loads the full ~220-row table (small enough to rank in C#), scores each code against the
  query (exact code match > code prefix match > description substring match > all query words
  present > partial word match), and returns the top N ordered by score. Deliberately simple
  and interview-explainable rather than clever.
- `Controllers/Icd10CodesController.cs` (`GET /api/icd10codes/search?q=...`) - thin wrapper
  around `IIcd10SearchService.SearchAsync`, returns `{ code, description }` JSON results.
- `Pages/Encounters/Workspace.cshtml` - new "ICD-10 code search" card (hidden when
  `IsReadOnly`): a debounced (300ms) free-text input that calls the search API and renders
  clickable results; clicking a result appends `"{code} - {description}"` as a new line into
  the `DraftNoteText` textarea, so a provider can quickly attach relevant diagnosis codes to
  the Assessment section before saving a note version.

## STEP 8 COMPLETE: Note versioning/audit trail persistence

- `Services/SoapNoteParser.cs` - static helper that splits the flat `DraftNoteText` string into
  Subjective/Objective/Assessment/Plan sections by matching `"SectionName:"` header lines via
  regex. Falls back to putting all text into `Subjective` if no headers are recognized, so
  content is never silently dropped even if the AI or a provider free-types without headers.
- `Services/IEncounterService.cs` / `EncounterService.cs`:
  - `SaveNoteVersionAsync(encounterId, providerId)` - parses the encounter's current
	`DraftNoteText`, creates a new immutable `NoteVersion` row with an auto-incremented
	`VersionNumber` (per encounter), writes an `AuditLog` entry ("Created" / `NoteVersion`),
	and marks the encounter `Status = Saved`. Returns `null` if the encounter isn't owned by
	the given provider or has no draft note text to finalize (nothing to save).
  - `GetNoteVersionsAsync(encounterId)` - lists all saved versions for an encounter (with the
	saving provider's name included), most recent first.
- `Pages/Encounters/Workspace.cshtml(.cs)`:
  - New "Save note (finalize version)" button (`OnPostSaveNoteAsync`) - first persists the
	current transcript/draft via the existing `SaveDraftAsync` (so nothing typed since the last
	manual "Save draft" click is lost), then calls `SaveNoteVersionAsync`.
  - Shows a warning alert if there's no draft text to finalize, or a success alert once a
	version is saved.
  - New "Saved note versions" table shows version number, saving provider, and saved-at
	timestamp for the encounter - the audit trail is now visibly surfaced to providers/admins,
	not just recorded silently in the database.
- NOTE: "Save draft" (existing, unversioned) and "Save note" (new, versioned) are intentionally
  both present: draft saves are cheap/frequent (typing/generation-in-progress persistence),
  while a note version is a deliberate, audited, immutable finalization action.

## STEP 7 COMPLETE: SOAP note streaming generation (SSE) with tool-calling

- `Program.cs` - the `IChatClient` singleton is now wrapped with
  `.UseFunctionInvocation()` (via `ChatClientBuilder`) so tool calls the model makes are
  automatically executed and fed back into the conversation, not just returned unexecuted.
- `Services/INoteGenerationService.cs` / `NoteGenerationService.cs`:
  - `GenerateNoteStreamAsync(encounterId, cancellationToken)` loads the encounter + patient,
	builds a system prompt instructing SOAP-note generation, and streams
	`_chatClient.GetStreamingResponseAsync(...)` results as an `IAsyncEnumerable<string>`.
  - Exposes a `get_patient_history` tool (via `AIFunctionFactory.Create`) bound by closure to
	the current patient's ID, so the model can look up up to 5 prior saved `NoteVersion` rows
	(Subjective/Objective/Assessment/Plan) for the same patient across *other* encounters, for
	context on returning patients - without always sending full history up front.
- `Controllers/NoteGenerationController.cs` (`GET /api/encounters/{encounterId}/generate-note`)
  - Streams the generated text as Server-Sent Events (`text/event-stream`). Only the encounter's
	owning provider may generate (uses `GetEncounterAsync(id, providerId, isAdmin: false)` -
	Admins can view drafts but do not generate on a provider's behalf).
  - Newlines in each streamed chunk are encoded as `\n` literal text (SSE `data:` lines cannot
	contain raw line breaks) and decoded client-side.
  - Sends a final `event: done` so the client knows to stop listening and re-enable the button.
- `Pages/Encounters/Workspace.cshtml` - added a "Generate note" button (hidden when
  `IsReadOnly`) that opens a browser `EventSource` against the SSE endpoint and appends each
  streamed chunk into the `DraftNoteText` textarea live, decoding `\n` back to real line breaks.
  The auth cookie is sent automatically since `EventSource` requests are same-origin.
- NOTE: generation only fills the `DraftNoteText` textarea in the browser - the provider must
  still click **Save draft** to persist it (existing Step 6 behavior). Saving a note as a
  finalized, versioned `NoteVersion` (with Subjective/Objective/Assessment/Plan split into
  separate fields and an audit trail) is Step 8, not yet implemented.

## STEP 6 COMPLETE: Provider encounter workspace backend

- FIX (post-completion): the `/Encounters` index page originally always called
  `GetEncountersForProviderAsync`, so Admins (who own no encounters themselves) saw an empty
  list instead of every provider's encounters. Added `IEncounterService.GetAllEncountersAsync()`
  (includes `Patient` + `Provider`), and `Index.cshtml(.cs)` now branches on
  `provider.Role == ProviderRole.Admin`: Admins see all encounters with a "Provider" column and
  no "start new encounter" form (Admins don't own encounters); Providers see only their own with
  the start-encounter form. `Workspace.cshtml(.cs)` already correctly handled the
  read-only/admin-viewing-another-provider case via `GetEncounterAsync(id, providerId, isAdmin)`.
- `Services/IPatientMatchingService.cs` / `PatientMatchingService.cs` - finds an existing
  `Patient` by case-insensitive first name + last name + date of birth, or creates a new one.
  This is how the app detects returning patients across encounters without a formal MRN.
- `Services/IEncounterService.cs` / `EncounterService.cs`:
  - `StartEncounterAsync` - matches/creates the patient, then creates a new `Encounter` row
	(status `Draft`, empty transcript) owned by the given provider.
  - `SaveDraftAsync` - updates `TranscriptText` / `DraftNoteText` / `UpdatedAtUtc` for an
	encounter, scoped to the owning provider (returns `null` if the encounter doesn't exist or
	belongs to a different provider - enforces "providers only see their own data").
  - `GetEncounterAsync` - fetches a single encounter with its `Patient` included; Admins can
	view any provider's encounter (read-only in the UI), non-admins only their own.
  - `GetEncountersForProviderAsync` - lists a provider's own encounters, most recently
	updated first.
- Both services registered as scoped in `Program.cs` (match the scoped `DbContext`).
- `Pages/Encounters/Index.cshtml(.cs)` - the workspace landing page: a "start new encounter"
  form (first name, last name, DOB) and a table of the signed-in provider's own encounters.
  Resolves the current provider via `IRoleResolutionService` on every request (not persisted in
  claims) since role resolution is cheap and always reflects the latest Provider row.
- `Pages/Encounters/Workspace.cshtml(.cs)` (route `/Encounters/Workspace/{id:guid}`) - lets the
  owning provider edit the transcript and in-progress draft note text and save continuously
  (`OnPostSaveDraftAsync`). If an Admin opens another provider's encounter, the page renders
  read-only (`IsReadOnly`) with the save button hidden - satisfies "Admin can view all
  encounters" without letting them edit a provider's draft.
- NOTE: SOAP note *generation* (AI streaming) and finalized `NoteVersion` saving are NOT yet
  implemented - `DraftNoteText` today is just a plain textarea a provider can type into
  manually. That's Step 7 (SSE streaming generation) and Step 8 (note versioning/audit trail).

## STEP 5 COMPLETE: Demo accounts + role resolution service

- `Data/DbSeeder.cs` - seeds 1 Admin + 3 Provider demo accounts into the `Providers` table on
  startup. Idempotent (no-op if any Providers already exist). Real test accounts created in
  Entra External ID and matched here by email:
  | Display Name | Email | Role |
  |---|---|---|
  | Admin | bcalderon_e94@outlook.com | Admin |
  | Provider1 (John Doe) | hospitalprovider1.gizmo280@passinbox.com | Provider |
  | Provider2 (Jane Doe) | hospitalprovider2.swimming970@passinbox.com | Provider |
  | Provider3 (Jack Doe) | brian.r.calderon@proton.me | Provider |
- `Program.cs` now calls `Database.MigrateAsync()` + `DbSeeder.SeedAsync()` right after
  `app.Build()`, before `app.Run()`.
- `Services/IRoleResolutionService.cs` / `RoleResolutionService.cs` - resolves the signed-in
  user's email (case-insensitive) to their `Provider` record. Deactivated providers resolve to
  `null` (same as unknown users), preserving their historical data but blocking access.
- New middleware in `Program.cs` (after `UseAuthentication()`, before `UseAuthorization()`):
  for any authenticated request, resolves the user's Provider record, stores it on
  `HttpContext.Items["CurrentProvider"]` for use by pages/layout, and redirects to
  `/AccessDenied` if no matching active Provider is found (except on public paths).
- `Pages/AccessDenied.cshtml` / `.cshtml.cs` - shown to unknown/deactivated users; displays
  their email and offers a sign-out link (public/anonymous page so it's always reachable).
- `Pages/Shared/_Layout.cshtml` - top-right header now shows `email (DisplayName)` using the
  `CurrentProvider` stashed in `HttpContext.Items`, falling back to just the email if no
  Provider record was resolved (shouldn't happen for authenticated users past the
  AccessDenied check, but keeps the layout defensive).
- NOTE: since `DbSeeder` only seeds once (skips if `Providers` table has any rows), if the
  DB was already seeded with old placeholder emails before these real accounts were created,
  either clear the `Providers` table manually or delete the initial migration's applied state
  before restarting, so the real emails get seeded.

## Homework Assignment Title: Note Keeper App
## Homework Assignment Number: 4
## Name: Brian Calderon
## Email(s): 
	- bcalder_e94@outlook.com
	- brc371@g.harvard.edu

-------------------------------------------------------------------------------------------------

## MIGRATION STATUS (Harvard subscription -> personal subscription) - IN PROGRESS

Old Harvard subscription expired/cancelled. Migrating all resources to a new personal
subscription "Azure subscription 1", resource group `rg-hospital-app-prod`.

### Completed so far
- App Service target: `app-hospital-prod` (Linux, dotnetcore 10.0), West Central US.
- New SQL Server/DB: `sql-hospital-server` / `sqldb-hospital-app`.
  - Connection string updated in appsettings.json + appsettings.Development.json to use
	`Authentication="Active Directory Default"` (no password).
  - New user-assigned managed identity `id_dbadmin_prod` created in `rg-hospital-app-prod`,
	attached to `app-hospital-prod` (User assigned identity).
  - Ran in SSMS against `sqldb-hospital-app`:
	CREATE USER [id_dbadmin_prod] FROM EXTERNAL PROVIDER;
	ALTER ROLE db_datareader/db_datawriter/db_ddladmin ADD MEMBER [id_dbadmin_prod];
  - App setting `DB_IDENTITY_CLIENT_ID` = Client ID of `id_dbadmin_prod` added to
	`app-hospital-prod` Environment variables.
  - Firewall "Allow Azure services" needs to stay enabled on `sql-hospital-server`.
- New Azure OpenAI resource: `ai-hospital-prod` (East US2), deployment name `gpt-5.4-mini`
  (Data Zone Standard, since Global Standard had no quota in this subscription).
  - appsettings.json / appsettings.Development.json AISettings.DeploymentUri updated to
	https://ai-hospital-prod.openai.azure.com/, DeploymentModelName = "gpt-5.4-mini".
  - API key placed in User Secrets locally, and in App Service env var `AISettings__ApiKey`.
- New Storage account: `sthospitalprod` (East US2, Standard LRS, key access disabled,
  Entra-only auth). New user-assigned managed identity `id_blob_storage` created, granted
  "Storage Blob Data Contributor" role on the storage account via IAM, attached to
  `app-hospital-prod`, client id set as `BLOB_IDENTITY_CLIENT_ID` app setting.
  - appsettings.json / appsettings.Development.json BlobStorageSettings.Uri updated to
	https://sthospitalprod.blob.core.windows.net (TenantId unchanged, same Entra tenant).
  - NOTE: BlobServiceClient + QueueStorageSettings appear to be UNUSED/dead code in the
	current project (no AttachmentsController/NotesController/QueueClient found) - safe to
	leave as-is or clean up later, not blocking.
- Build verified successful after all appsettings changes.

### Still TODO
1. ~~Run EF Core migrations against the new empty `sqldb-hospital-app` database~~ - DEFERRED.
   No `Migrations` folder exists in the project yet (none were ever created), and the DB is
   just a staging placeholder with no schema/data to preserve. Will create the initial
   migration once the EF models/schema are finalized (`dotnet ef migrations add InitialCreate`,
   then `dotnet ef database update` or apply via Program.cs `Database.Migrate()`).
2. Publish `Hospital-AI` project to `app-hospital-prod` App Service from Visual Studio
   (Publish profile -> Azure App Service Linux -> rg-hospital-app-prod -> app-hospital-prod).
3. After publish, verify app starts correctly (check Log stream) - watch for Blob/DB
   managed identity auth issues.
4. Repo being renamed/copied from `Hospital-AI` to a new project "Hospital AI Prod" via
   the dotnet-project-renamer skill - once reopened in the new location, confirm this file
   carried over, and confirm appsettings.json still has the values above (renamer should not
   touch these, but verify).
5. Optional cleanup: remove unused BlobServiceClient registration / QueueStorageSettings
   from Program.cs and appsettings if truly dead code.

-------------------------------------------------------------------------------------------------

## HW4 Design Decisions

### Managed Identity Approach

Two **user-assigned managed identities** are used — one scoped to blob storage and one scoped to the Azure SQL Database. Keeping them separate follows the principle of least privilege: each identity carries only the role assignments it needs.

| Identity name  | Purpose                        | Azure role / permission                            |
|----------------|--------------------------------|----------------------------------------------------|
| `id_blobadmin` | Azure Blob Storage access +Sotrage queues     | Storage Blob Data Contributor on the storage account |
| `id_dbadmin`   | Azure SQL Database access      | `db_datareader` / `db_datawriter` / `db_ddladmin` in `sqldb-cscie94-2026` |

Both identities are attached to the App Service (`hw4notekeeper`) as user-assigned managed identities.

---

### Environment Variables (App Service Application Settings)

The **client IDs** of the two managed identities are injected into the App Service as Application Settings (environment variables). `Program.cs` reads them at startup to tell `DefaultAzureCredential` / `SqlConnectionStringBuilder` which identity to use.

| Environment variable name  | Value source                                      | Used by                              |
|----------------------------|---------------------------------------------------|--------------------------------------|
| `BLOB_IDENTITY_CLIENT_ID`  | Client ID of `id_blobadmin` (from Entra ID)       | `DefaultAzureCredentialOptions.ManagedIdentityClientId` when building `BlobServiceClient` |
| `DB_IDENTITY_CLIENT_ID`    | Client ID of `id_dbadmin` (from Entra ID)         | `SqlConnectionStringBuilder.UserID` with `Authentication=ActiveDirectoryManagedIdentity` |

> These variables are **not** present in any `appsettings*.json` file. They are set exclusively in the App Service **Configuration → Application settings** blade so they are never checked into source control.

--- 

### Note Delete – Blob Storage Approach
The **delete-container-at-once** alternative described in spec section 2.5 is used.
`DeleteIfExistsAsync()` is called on the `BlobContainerClient` for the note's container,
which atomically removes the container and every blob inside it in a single SDK call.
- If the container does not exist the call returns `false` and no error is raised (covers spec 2.5.4).
- If the call throws, the error is logged but note/tag deletion proceeds normally (covers spec 2.5.5).
- Note and tag deletion are handled by a single `SaveChangesAsync()` call;
  EF's `DeleteBehavior.Cascade` on the Tag → Note relationship removes tags automatically.
  Any database failure here is logged and returns HTTP 500 (covers spec 2.5.2).

### Note Delete Enhancement (1.5) – Zip Blob Storage Approach
The **delete-container-at-once** alternative described in spec section 1.5 is used for both the
attachment container and the zip container.
- The note and tags are deleted from the database **first**. If the database delete fails, HTTP 500
  is returned and no blob storage deletions are attempted (covers spec 1.5.6.2).
- `DeleteIfExistsAsync()` is called on the `BlobContainerClient` for the attachment container
  (`{noteId}`), atomically removing the container and all attachment blobs in a single SDK call
  (covers spec 1.5.2 / 1.5.3).
- `DeleteIfExistsAsync()` is then called on the zip container (`{noteId}-zip`), atomically
  removing the container and all zip blobs in a single SDK call (covers spec 1.5.4 / 1.5.5).
- If either container deletion fails the error is logged but a success response is still returned
  to the caller (covers spec 1.5.6.1).

---

## Extra Credit Completed

### Extra Credit 2: Text-Based Console App Chat Interface for NoteKeeper

**Completed.** A .NET 10 console application named `Hospital-AIChat` was added to the `Hospital-AI` solution. It provides a multi-turn, AI-powered natural language interface for interacting with the NoteKeeper REST API using Azure OpenAI tool-calling (function calling).

**Model used:** `gpt-5.3-chat`
- Deployment URI: `https://bcald-mn883lq8-eastus2.cognitiveservices.azure.com/openai/responses?api-version=2025-04-01-preview`
- API Key: `2ppGVXOOwRbd4ctS3UnXoE54KTfHsjqxS12Rc1l1V0G8JDo2mL33JQQJ99CCACHYHv6XJ3w3AAAAACOGp708`

**Projects added to solution:**
- `Hospital-AIChat` — .NET 10 console application

**Key files implemented:**
- `NoteKeeperApiClient.cs` — HTTP client wrapper that builds request URLs from `NoteKeeperSettings.GetActiveBaseUrl()`, supporting runtime endpoint switching without restart.
- `NoteKeeperTools.cs` — Defines four AI tool functions registered via `AIFunctionFactory.Create()`:
  - `GetAllNotesAsync` — Retrieves all notes (optionally filtered by tag) and returns a 1-based numbered list so users can reference notes by position in follow-up messages (e.g. "give me note 2").
  - `GetNoteByIdAsync` — Retrieves full details for a single note by GUID, including Summary, Details, Tags, timestamps, and a numbered list of attachments.
  - `DownloadAttachmentAsync` — Downloads an attachment blob via the NoteKeeper API and saves it to the local filesystem (defaults to the user's Downloads folder). Reports the full saved path back to the user.
  - `WriteDataToFileAsync` — Writes arbitrary text content to a local file (e.g. a markdown file). Supports filenames only (saved to CWD) or fully qualified paths.
- `ConsoleHelpers.cs` — Utility methods for color-coded console output.
- `Program.cs` — Hosts the interactive chat loop with multi-turn conversation history. Supports three built-in commands:
  - `/bye` — exit the application
  - `/clear` — reset conversation history and redisplay the startup banner
  - `/switch` — toggle the active API endpoint between Localhost and Azure at runtime

**Endpoint support (spec requirement to support two endpoints):**
- `NoteKeeperSettings` in `appsettings.json` holds both `LocalhostBaseUrl` and `AzureBaseUrl`.
- `ActiveEndpoint` controls which URL is used; the `/switch` command toggles it at runtime.
- `NoteKeeperApiClient` calls `GetActiveBaseUrl()` on every request so the switch takes effect immediately with no restart required.

**Multi-turn conversation context:**
- All chat messages (system, user, assistant) are accumulated in a `List<ChatMessage>` across turns.
- The system prompt instructs the model to number all listed notes and attachments so users can reference them by number in follow-up prompts (e.g. "give me the details for note 2").
- `/clear` resets the list and re-adds the system prompt, starting a fresh context.

**Configuration:**
- AI settings (deployment URI, API key, model name) are stored in `appsettings.json` under the `AISettings` section.
- NoteKeeper API settings (localhost URL, Azure URL, active endpoint) are stored under the `NoteKeeperSettings` section.
- API keys should be stored in User Secrets locally and environment variables in production; they are not committed to source control.

---

### Extra Credit 3: Managed Identities for Azure Storage Queue Authentication

**Completed.** The user-assigned managed identity `id_blobadmin` is used for authenticated access
to Azure Storage Queues in both the Azure Function and the App Service.

**Azure portal configuration:**
- `id_blobadmin` is attached as a user-assigned managed identity to:
  - App Service: `hw4notekeeper`
  - Function App: `func-hw4-notekeeper`
- `id_blobadmin` is granted the **Storage Queue Data Contributor** role on storage account `sthw4notekeeper`

**Function App environment variables (spec 1.5):**
- `AzureWebJobsStorage` — connection string (kept for runtime internal use, spec 1.4)
- `AzureWebJobsStorage__queueServiceUri` — `https://sthw4notekeeper.queue.core.windows.net`
- `AzureWebJobsStorage__clientId` — client ID of `id_blobadmin` (`994cd445-32c9-4e83-a70f-7ecebb5028dd`)

**App Service queue access (spec 1.6):**
- `QueueClient` in `Program.cs` is registered using `DefaultAzureCredential` configured with
  `id_blobadmin` via `ManagedIdentityClientId`, authenticating to the queue without a connection string.

**Foundry configuration:**
- Resource group: `rg_aifoundry`
- Model Name: gpt-5.3-chat
- URI: https://bcald-mn883lq8-eastus2.cognitiveservices.azure.com/openai/responses?api-version=2025-04-01-preview
- Key: 2ppGVXOOwRbd4ctS3UnXoE54KTfHsjqxS12Rc1l1V0G8JDo2mL33JQQJ99CCACHYHv6XJ3w3AAAAACOGp708

## Custom Azure resource abbreviations:
	 - rg_hw4_notekeeper: Resource group for the Note Keeper project
	 - rg_aifoundry: Resource group for Microsoft Foundry resources

## Microsoft Foundry:
	- Resource group: rg_aifoundry
	- Project Name: GPT-model
	- Uri Azure OpenAI Service: https://ai-cscie94-foundry-demo-01.openai.azure.com/
	- API Key: "1qfnety8D9az5J7yhwEwn7Wjh4DUpdNA9At8ftrc5ttqpShaXFuiJQQJ99CBACYeBjFXJ3w3AAAAACOGzUPk"

## Web App:
	- URL to Azure App Service: hw4notekeeper.azurewebsites.net
	- App service plan: ASP-rgclassdemo01-b91b
		*Resource group for app service plan: rg_hw4_notekeeper

## Azure SQL Database:
	- Server name: sql-cscie-94-2026
	- Server URL: sql-cscie-94-2026.database.windows.net
	- Database name: sqldb-hw4-notekeeper
	- Admin username: sqldbadmin
	- Admin password: Kroemer#082528

## Azure blob storage:
	- Storage account name: cscie942026bcalderon
	- Resource group: rg_hw3_notekeeper
	- Location: eastus

### Azure SQL Database User Creation Script to add users to Azure SQL Database and assign them to roles in the database -->

-- You need to login using the Azure Active Directory Admin account for your Azure SQL Server Instance
-- You must login using Microsoft Entra MFA / Microsoft Entra ID – Universal with MFA support
-- This script will add the users listed below to your Azure SQL Server Instance
-- and assign them to the db_datareader, db_datawriter, and db_ddladmin roles in the database
-- You will need to run this script in the context of the database you want to add the users to
-- Replace the <user principal name> with the User Principal Name for each user listed below
-- The User principal name can be found in the Microsoft Entra ID Users listing in your Microsoft Entra ID Tenant
-- In the Overview tab for each user
-- It is in the format of <user>@<tenant>.onmicrosoft.com
-- Example: jon001sox@gmail.com#EXT#@<your domain>.onmicrosoft.com

-- Jonathan Franck
CREATE USER [jfranckHarvard2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [jfranckHarvard2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_datawriter ADD MEMBER [jfranckHarvard2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_ddladmin ADD MEMBER [jfranckHarvard2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com];


-- Max Eringros
CREATE USER [meringros_gmail.com#EXT#@bcalderone94.onmicrosoft.com] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [meringros_gmail.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_datawriter ADD MEMBER [meringros_gmail.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_ddladmin ADD MEMBER [meringros_gmail.com#EXT#@bcalderone94.onmicrosoft.com];


-- Jessica Pratt
CREATE USER [phvdta_outlook.com#EXT#@bcalderone94.onmicrosoft.com] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [phvdta_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_datawriter ADD MEMBER [phvdta_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_ddladmin ADD MEMBER [phvdta_outlook.com#EXT#@bcalderone94.onmicrosoft.com];


-- Nancy Forero
CREATE USER [Nancy.scuba_outlook.com#EXT#@bcalderone94.onmicrosoft.com] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [Nancy.scuba_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_datawriter ADD MEMBER [Nancy.scuba_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_ddladmin ADD MEMBER [Nancy.scuba_outlook.com#EXT#@bcalderone94.onmicrosoft.com];


-- Joseph Ficara
CREATE USER [jficarainstructor2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [jficarainstructor2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_datawriter ADD MEMBER [jficarainstructor2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com];
ALTER ROLE db_ddladmin ADD MEMBER [jficarainstructor2026_outlook.com#EXT#@bcalderone94.onmicrosoft.com];

GO

### Attribution(s):
Copilot was used to develope these initial parts of the code:
	- Swagger UI setup: intializing the Swagger UI in the Program.cs file
	- Defining the Note model and the NotesController with basic CRUD operations
	- Setting up the launchSettings.json to launch the browser on the correct port and URL
	- Defining the API endpoints and their expected behavior in the NotesController
	- Using LINQ to query the in-memory list of notes for the GET and DELETE operations
	- Defining Note.cs with properties and constructors for the Note model
	- Defining CreateNoteRequest and UpdateNoteRequest classes to handle the request bodies for creating and updating notes
	- XML comments for API documentation
	- Asking questions to learn about the behavior of the application and how to set up the development environment correctly
	- I manually started code when I felt I understood the concepts and had a clear idea of how to implement the functionality, 
	  but I used Copilot to assist with the initial code structure and to provide suggestions for implementing the API endpoints 
	  and setting up the Swagger UI. I implemented the code for DELETE on my own. 

Copilot skills:
	- skill-creator: Used to create the docx-to-markdown skill.
	- docx-to-markdown: Used to convert the hw 3 assignment instructions from the docx format to markdown format for easier reference.
	- theme-factory: Enabled but not used in this project
	- web-artifacts-builder: Enabled but not used in this project
	- webapp-testing: Enabled but not used in this project

Copilot Plugins:
	- 