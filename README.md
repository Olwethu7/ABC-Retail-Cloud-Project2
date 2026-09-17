# ABC Retail Cloud — Project 2

Student Number: ST10344453


This solution contains two projects:

1. **AzureStorageProject** — the ASP.NET Core MVC web application (from Project 1),
   extended with a **Transactions** feature (`TransactionController`) that calls
   four Azure Functions instead of talking to storage directly.
2. **AzureStorageProject.Functions** — a new Azure Functions app (isolated worker,
   .NET 8) with four HTTP-triggered functions:
   - `StoreTransactionTable` — writes a transaction record to Azure Table Storage.
   - `UploadTransactionBlob` — writes a transaction receipt/invoice to Blob Storage.
   - `TransactionQueue` — writes to (`?action=write`) and reads from
     (`?action=read`) the `transactionqueue` Azure Queue.
   - `WriteTransactionFile` — writes a transaction log entry to Azure Files.

## Running locally

1. Open `AzureStorageProject.sln` in Visual Studio 2022 (17.8+, with the
   **Azure development** workload and **Azure Functions Tools** installed).
2. In `AzureStorageProject.Functions/local.settings.json`, replace the
   placeholder connection string with your real Azure Storage connection
   string (Azure Portal → Storage account → Access keys).
3. Right-click the solution → **Configure Startup Projects** → set both
   `AzureStorageProject` and `AzureStorageProject.Functions` to start
   (or start the Functions project first, then the web project).
4. Start the Functions project. Note the local base URL it prints
   (usually `http://localhost:7071`) and the function keys shown in the
   console for each function (only needed if you keep `AuthorizationLevel.Function`).
5. In `AzureStorageProject/appsettings.Development.json`, make sure
   `FunctionsSettings:BaseUrl` points at that local URL.
6. Start the web app and browse to **Transactions (Functions)** in the nav bar.

## Publishing to Azure

See the step-by-step guide provided with this submission
(`ST10344453__Project2.docx`, Appendix A) for the exact Azure Portal
and Visual Studio publish steps for both the Function App and the App Service.

## Security note

The Azure Storage account key committed in `appsettings.json` (inherited from
Project 1) should be treated as compromised since it has been shared in
submitted files/repositories. Rotate it in the Azure Portal
(Storage account → Access keys → Rotate key) and move it to User Secrets /
App Service Application Settings rather than source control.
