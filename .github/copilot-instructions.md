# Copilot Instructions

## Build and packaging commands

- Restore: `dotnet restore .\StreamDeckMicrosoftFabric.sln`
- Build (preferred for CLI work): `dotnet build .\StreamDeckMicrosoftFabric.sln --configuration Release --no-restore`
- Validation command currently available in the repo: `dotnet test .\StreamDeckMicrosoftFabric.sln --configuration Release --no-build`
- Package a release artifact the same way the release workflow does:
  1. `npm install -g @elgato/cli@latest`
  2. `dotnet restore .\StreamDeckMicrosoftFabric.csproj`
  3. `dotnet build .\StreamDeckMicrosoftFabric.csproj --configuration Release`
  4. `dotnet publish .\StreamDeckMicrosoftFabric.csproj --configuration Release --runtime win-x64 --self-contained --output .\publish`
  5. Copy `.\publish\*`, `manifest.json`, `images\`, and `property_inspector\` into `.\net.fabricdeck.microsoftfabric.sdPlugin`
  6. `streamdeck pack .\net.fabricdeck.microsoftfabric.sdPlugin --output "FabricDeck-<version>.streamDeckPlugin"`

Use Release builds for unattended CLI work. Debug builds run the `PostBuild` target from `StreamDeckMicrosoftFabric.csproj`, which invokes `RegisterPluginAndStartStreamDeck.ps1` and relaunches the Stream Deck desktop app.

No dedicated test project or lint configuration is checked in. `dotnet test` succeeds because the solution loads, but there are no committed tests to run individually.

## High-level architecture

- `Program.cs` boots a generic host, registers `HttpClient` and logging, then uses `AddStreamDeck(..., typeof(Program).Assembly)` so StreamDeckLib discovers all action classes in the assembly.
- `manifest.json` is the plugin contract for Stream Deck. It defines the action list, shared property inspector, plugin metadata, and the UUIDs that must match the `[ActionUuid]` attributes in the C# action classes.
- Each concrete action class (`NotebookActionRunner`, `DatapipelineActionRunner`, `DataflowRunner`, `DeployActionRunner`, `ConsumptionRunner`, `UserDataFunctionRunner`) is intentionally thin. The class exists mainly to provide a unique Stream Deck action UUID and map that action to a `SupportedActions` value.
- `BaseAction.cs` contains the real runtime behavior shared by every button action: key press handling, long-press vs tap dispatch, settings validation, login checks, starting the Fabric operation, updating button images/titles, persisting settings, background polling, and error surfacing.
- `Services\LoginService.cs` is responsible for acquiring and caching the Fabric access token. It supports two auth paths: interactive browser login and confidential-client/service-principal login.
- `Services\FabricService.cs` owns the Fabric REST API calls. It starts notebook/pipeline/dataflow runs, triggers deployment pipeline deployments, invokes User Data Functions, queries capacity usage, stores the last operation location, and translates Fabric job states into the `JobRunStatus` used by `BaseAction`.
- `property_inspector\property_inspector.html` and `property_inspector\js\property-inspector.js` are the settings UI. The JavaScript reads and writes the same `settingsModel` fields as `FabricSettingsModel`, and changes the visible fields and labels depending on which action UUID opened the inspector.
- The project file copies `manifest.json`, `images\`, `appsettings.json`, and the property inspector assets into the output. Release packaging then wraps those published files into a `.sdPlugin` folder and packs it with the Elgato CLI.

## Key conventions

- Keep action runner classes minimal. When adding a new action, wire it through all of these surfaces together: `manifest.json`, a dedicated C# action class with its own `[ActionUuid]`, `SupportedActions`, `BaseAction` validation/execution, `FabricService`, and the property inspector UI if settings differ.
- The UUID wiring is cross-file and must stay exact: `manifest.json` action UUIDs, C# `[ActionUuid]` values, and `ACTION_IDS` in `property-inspector.js` all need to match.
- `FabricSettingsModel` is persisted through StreamDeckLib settings serialization, so it uses primitive/string properties rather than enums. Keep persisted enum-like values as `int` fields (`LoginMethod`, `UpdateStatusEverySecond`) and convert them with the companion enums outside the model.
- Do not assume `ResourceId` always means the same thing. Its meaning depends on the action:
  - notebook / pipeline / dataflow: Fabric item id
  - consumption: capacity name
  - deploy: deployment pipeline id
  - user data function: public function URL
- Action-specific validation belongs in `BaseAction.IsSettingsValid()`, not in the tiny action runner classes.
- Error handling is expected to flow through `SettingsModel.ErrorMessage`, `Manager.ShowAlertAsync(...)`, and button image updates in `BaseAction.OnError()`. The property inspector renders `ErrorMessage`, so preserving that path matters for user-visible diagnostics.
- Polling behavior is split across layers: `FabricService` tracks the last job URL and current status, while `BaseAction` owns the polling loop and stops it when the action leaves the deck or the job finishes.
- User Data Function invocations are treated as synchronous success/failure calls. They clear the stored operation URL and do not rely on later polling.
- The default polling frequency for new settings comes from `property-inspector.js` (`UpdateStatusEverySecond = 2`, every minute), not from `FabricSettingsModel`, whose C# default is `0`.
- Logging is configured through `appsettings.json` to write Serilog output to `log/fabricplugin.log`. If an action only shows a red question mark in Stream Deck, that log file is the first place to inspect.
