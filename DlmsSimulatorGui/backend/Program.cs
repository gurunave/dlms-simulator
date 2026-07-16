using DlmsSimulatorGui.Api.Hubs;
using DlmsSimulatorGui.Api.Simulator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<MeterManager>();
builder.Services.AddCors(o => o.AddPolicy("dev", p =>
    p.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

var app = builder.Build();

// Serve the built React app (wwwroot) with SPA fallback.
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("dev");

// ---- Seed bundled templates from the Gurux simulator on first run ----------
SeedTemplates(app);

var api = app.MapGroup("/api");

// Templates -----------------------------------------------------------------
api.MapGet("/templates", (MeterManager m) => Results.Ok(m.ListTemplates()));

api.MapPost("/templates", async (HttpRequest req, MeterManager m) =>
{
    if (!req.HasFormContentType)
    {
        return Results.BadRequest("Expected multipart form upload.");
    }
    var form = await req.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file == null || Path.GetExtension(file.FileName).ToLowerInvariant() != ".xml")
    {
        return Results.BadRequest("Please upload an .xml template file.");
    }
    var dest = m.ResolveTemplatePath(file.FileName);
    await using var fs = File.Create(dest);
    await file.CopyToAsync(fs);
    return Results.Ok(new { name = Path.GetFileName(dest) });
});

// Meters --------------------------------------------------------------------
api.MapGet("/meters", (MeterManager m) => Results.Ok(m.List()));

api.MapGet("/meters/{id}", (string id, MeterManager m) =>
    m.Get(id) is { } info ? Results.Ok(info) : Results.NotFound());

api.MapPost("/meters", (CreateMeterRequest req, MeterManager m) =>
{
    try { return Results.Ok(m.Create(req)); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
});

api.MapPost("/meters/{id}/start", (string id, MeterManager m) =>
{
    try { return Results.Ok(m.Start(id)); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
});

api.MapPost("/meters/{id}/stop", (string id, MeterManager m) =>
{
    try { return Results.Ok(m.Stop(id)); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
});

api.MapDelete("/meters/{id}", (string id, MeterManager m) =>
{
    m.Delete(id);
    return Results.NoContent();
});

// COSEM objects -------------------------------------------------------------
api.MapGet("/meters/{id}/objects", (string id, MeterManager m) =>
{
    try { return Results.Ok(m.GetObjects(id)); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

api.MapPut("/meters/{id}/objects/{ln}", (string id, string ln, SetAttributeRequest req, MeterManager m) =>
{
    try { return Results.Ok(m.SetAttribute(id, ln, req)); }
    catch (KeyNotFoundException) { return Results.NotFound(); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapHub<SimulatorHub>("/hub/simulator");

// SPA fallback so client-side routes resolve to index.html.
app.MapFallbackToFile("index.html");

app.Run();

// ---------------------------------------------------------------------------
static void SeedTemplates(WebApplication app)
{
    var mgr = app.Services.GetRequiredService<MeterManager>();
    if (Directory.EnumerateFiles(mgr.TemplatesDir, "*.xml").Any())
    {
        return; // already seeded
    }
    // The cloned Gurux repo sits two levels up from the backend content root.
    var repoSim = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath,
        "..", "..", "Gurux.DLMS.Net", "Gurux.DLMS.Simulator.Net"));
    if (!Directory.Exists(repoSim))
    {
        return;
    }
    var sources = new List<string>();
    sources.AddRange(Directory.EnumerateFiles(repoSim, "*.xml"));
    var tmplDir = Path.Combine(repoSim, "Templates");
    if (Directory.Exists(tmplDir))
    {
        sources.AddRange(Directory.EnumerateFiles(tmplDir, "*.xml"));
    }
    foreach (var src in sources)
    {
        try
        {
            File.Copy(src, Path.Combine(mgr.TemplatesDir, Path.GetFileName(src)), overwrite: false);
        }
        catch { /* ignore individual copy errors */ }
    }
}
