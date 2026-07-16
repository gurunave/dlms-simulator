# Extending the DLMS Simulator

Copy-pasteable recipes for the most likely changes. Read
[../CLAUDE.md](../CLAUDE.md) and [ARCHITECTURE.md](ARCHITECTURE.md) first.
Paths are relative to `DlmsSimulatorGui/`.

**Golden rule:** never edit `Gurux.DLMS.Net/**` (vendored GPL). Extend by
subclassing or by adding code in `backend/` and `frontend/`.

---

## Recipe 1 — Add a REST endpoint
1. Put the logic on `MeterManager` (backend/Simulator/MeterManager.cs), e.g.:
   ```csharp
   public MeterInfo Rename(string id, string name)
   {
       var inst = Require(id);
       inst.Config.Name = name;
       BroadcastStatus(inst);          // optional: notify UI
       return ToInfo(inst);
   }
   ```
2. Map it in `backend/Program.cs` next to the others (keep endpoints thin):
   ```csharp
   api.MapPost("/meters/{id}/rename", (string id, RenameRequest req, MeterManager m) =>
   {
       try { return Results.Ok(m.Rename(id, req.Name)); }
       catch (KeyNotFoundException) { return Results.NotFound(); }
       catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
   });
   ```
3. Add the request DTO to `backend/Simulator/Models.cs`, and a method in
   `frontend/src/api.js`:
   ```js
   rename: (id, name) => req('POST', `/api/meters/${id}/rename`, { name }),
   ```

## Recipe 2 — Add a new realtime (SignalR) event
1. Build + push the payload from `MeterManager` (it already holds `IHubContext`):
   ```csharp
   _hub.Clients.All.SendAsync("meterLog", new { meterId = id, line });
   ```
   (Add a typed model in `Models.cs` if the shape is reused.)
2. Subscribe in `frontend/src/signalr.js`:
   ```js
   conn.on('meterLog', (ev) => onMeterLog && onMeterLog(ev));
   ```
   thread the callback through `connectHub({...})` and handle it in
   `frontend/src/App.jsx` (add state + a handler like the existing `onActivity`).

## Recipe 3 — Support editing more COSEM attribute value types
Value marshalling lives in `MeterManager.ConvertValue(raw, existing)`. It switches
on the existing value's .NET type. Add a `case` for the type you need (e.g. a
`GXDateTime`, an enum, or a structured value), converting the incoming string.
Reads already format any value via `ValueFormatter.ToDisplay` in `SimMeter.cs`
(hex for `byte[]`, bracketed lists for arrays) — extend that for nicer display.

## Recipe 4 — Add / manage meter templates
- A template is a Gurux COSEM-object XML (same format Gurux tools emit).
- **Drop-in:** put the `.xml` in `backend/templates/` (dev) or next to the exe in
  `templates/` (published). It appears in `GET /api/templates`.
- **Upload:** already supported via `POST /api/templates` (multipart, field `file`)
  and the UI's upload button in `CreateMeter.jsx`.
- To bundle a new default template into releases, it just needs to live in the
  vendored `Gurux.DLMS.Simulator.Net` folder (the `.csproj` `<Content>` glob copies
  `*.xml` and `Templates/*.xml` into the build output).

## Recipe 5 — Add a UI view/panel
1. Create a component in `frontend/src/components/`. Take data + callbacks as props
   (no global store). Use `api.js` for calls, never `fetch` directly.
2. Render it from `App.jsx` and give it whatever slice of `meters`/`activity` it
   needs. Add styles to `frontend/src/styles.css` (plain CSS, existing variables).

## Recipe 6 — Add a platform to the release
Edit the matrix in `.github/workflows/release.yml`:
```yaml
- { os: ubuntu-latest, rid: linux-arm64, name: linux-arm64 }
```
Packaging is branched by `runner.os` (zip on Windows, tar.gz + `chmod` on unix), so
a new self-contained RID needs no other change. Non-Windows builds use
`packaging/unix/`; Windows uses `packaging/win-x64/`.

---

## Larger features (design sketches)

### A. Visual COSEM object editor (add / remove objects)
Today you can edit attribute **values**; adding/removing **objects** is the natural
next step (the "full template editor").
- Backend: add `MeterManager` methods `AddObject(id, type, ln)` and
  `RemoveObject(id, ln)` that mutate `inst.Meter.Items` (a `GXDLMSObjectCollection`)
  — create objects via `GXDLMSObjectFactory` / the typed constructors
  (`new GXDLMSRegister(ln)`, etc.), then `Items.Save(templatePath)`.
- Note: some structural changes only take full effect on meter (re)start, since
  associations/capture-objects are wired at `Initialize`. Consider a "restart to
  apply" hint, or stop/start the meter inside the operation.
- Frontend: an "Add object" dialog (pick `ObjectType` + OBIS) and a delete action
  per row in `ObjectTable.jsx`.

### B. Profile-generic / load-profile buffer viewer
`GXDLMSProfileGeneric` objects have a `Buffer` (rows) and `CaptureObjects`.
- Backend: extend `GetObjects` (or add `/api/meters/{id}/objects/{ln}/buffer`) to
  return capture-object columns + buffer rows for profile-generic objects.
- Frontend: render a table when the selected object is a `ProfileGeneric`.

### C. Persist meters across restarts
Meters currently live only in memory (`MeterManager._meters`). To persist, serialize
the `CreateMeterRequest` configs (JSON file next to the exe) on change, and reload +
optionally auto-start them on startup in `Program.cs`.

### D. Per-meter trace / packet log
`SimMeter` already sees every read/write; capture raw DLMS frames by overriding more
of the Gurux hooks (or wiring `GXNet.OnTrace`) and stream them as a `meterLog`
SignalR event (Recipe 2) into a per-meter log panel.

---

## Verifying a change
There is no unit-test suite. After a change:
1. `cd backend && dotnet run --urls http://localhost:5100` (or the single-app build).
2. Exercise the flow in the UI, **and/or** run the real client:
   `cd tools/DlmsProbe && dotnet run 127.0.0.1 <meter-port>`.
3. Confirm the expected `activity`/`meterStatus` events show in the UI's live pane.
Reads should surface in the activity log; edits should change what the client reads.
