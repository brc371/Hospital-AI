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