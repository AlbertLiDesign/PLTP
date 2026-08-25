using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using PLTP;
using PLTP.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<JobStore>();
builder.Services.AddSingleton<SampleCatalog>();

// A BESO result is a large text file - the million-tetrahedron chair runs to
// hundreds of megabytes - so the default 128 MB multipart cap would reject the
// interesting cases. This is a tool the user runs on their own machine.
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = long.MaxValue;
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartHeadersLengthLimit = int.MaxValue;
});
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = null);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var app = builder.Build();

app.UseDefaultFiles();

// Revalidate rather than cache. wwwroot is served straight from the project
// directory, so editing the HTML, CSS or JS and reloading is the normal way to
// work on this - and a browser holding an old ES module makes that silently not
// work. The ETag still turns each check into a 304.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
        ctx.Context.Response.Headers.CacheControl = "no-cache, must-revalidate",
});

var samples = app.Services.GetRequiredService<SampleCatalog>();
var store = app.Services.GetRequiredService<JobStore>();

app.MapGet("/api/samples", () => Results.Ok(new
{
    dataRoot = samples.DataRoot,
    // Null when there is no copy in paper/. The page fetches this at boot
    // anyway, so the local-PDF link costs no extra request.
    paperUrl = samples.PaperPath != null ? "api/paper" : null,
    samples = samples.Available()
}));

// The published paper, when a copy is in paper/. The citation is in the page
// either way; this only backs the "PDF" link, which stays hidden without it.
app.MapGet("/api/paper", () =>
{
    var path = samples.PaperPath;
    if (path == null || !File.Exists(path))
        return Results.NotFound(new { error = "No PDF in paper/." });

    return Results.File(path, "application/pdf",
        fileDownloadName: null,           // inline, so the browser opens it in a tab
        enableRangeProcessing: true);
});

app.MapPost("/api/jobs", async (HttpRequest http) =>
{
    if (!http.HasFormContentType)
        return Results.BadRequest(new { error = "Expected a multipart form." });

    var form = await http.ReadFormAsync();
    var req = ReadRequest(form);

    var job = new ExtractionJob { Request = req };
    string modelPath, sensitivityPath;

    var sampleId = form["sample"].ToString();
    if (!string.IsNullOrWhiteSpace(sampleId))
    {
        var resolved = samples.Resolve(sampleId);
        if (resolved == null)
            return Results.BadRequest(new { error = $"No sample called \"{sampleId}\"." });

        (modelPath, sensitivityPath) = resolved.Value;
        var meta = samples.Find(sampleId)!;
        req.SourceName = meta.Name;
    }
    else
    {
        var model = form.Files["model"];
        var sensitivity = form.Files["sensitivity"];
        if (model == null || sensitivity == null)
            return Results.BadRequest(new
            {
                error = "Both a model file and a sensitivity file are needed."
            });

        var workspace = store.CreateWorkspace(job.Id);
        modelPath = Path.Combine(workspace, Sanitise(model.FileName, "model.txt"));
        sensitivityPath = Path.Combine(workspace, Sanitise(sensitivity.FileName, "sensitivity.txt"));

        await using (var s = File.Create(modelPath)) await model.CopyToAsync(s);
        await using (var s = File.Create(sensitivityPath)) await sensitivity.CopyToAsync(s);

        req.SourceName = model.FileName;
    }

    store.Submit(job, modelPath, sensitivityPath);
    return Results.Ok(new { id = job.Id });
});

app.MapGet("/api/jobs/{id}", (string id, int? since) =>
{
    var job = store.Get(id);
    if (job == null) return Results.NotFound(new { error = "No such job (it may have been evicted)." });

    var log = job.Log_();
    return Results.Ok(new
    {
        id = job.Id,
        state = job.State.ToString().ToLowerInvariant(),
        stage = job.Stage,
        progress = job.Progress,
        elapsedMs = job.ElapsedMs,
        error = job.Error,
        result = job.Result,
        logCount = log.Count,
        log = log.Skip(since ?? 0).ToArray()
    });
});

app.MapPost("/api/jobs/{id}/cancel", (string id) =>
{
    var job = store.Get(id);
    if (job == null) return Results.NotFound(new { error = "No such job." });
    job.Cancellation.Cancel();
    return Results.Ok(new { cancelled = true });
});

app.MapGet("/api/jobs/{id}/mesh", (string id) =>
{
    var job = store.Get(id);
    if (job?.Packed == null) return Results.NotFound(new { error = "No surface for that job." });
    return Results.Bytes(job.Packed, "application/octet-stream");
});

app.MapGet("/api/jobs/{id}/download/{format}", (string id, string format) =>
{
    var job = store.Get(id);
    if (job?.Mesh == null) return Results.NotFound(new { error = "No surface for that job." });

    format = format.ToLowerInvariant();
    if (format is not ("obj" or "stl"))
        return Results.BadRequest(new { error = "Format must be obj or stl." });

    // Written to a scratch file rather than a buffer: a converged surface reaches
    // millions of faces, and the OBJ text for that is far too large to hold twice.
    var temp = Path.Combine(Path.GetTempPath(), $"pltp-{job.Id}-{Guid.NewGuid():N}.{format}");
    if (format == "obj") Export.WriteObj(job.Mesh, temp);
    else Export.WriteStl(job.Mesh, temp);

    var name = Path.GetFileNameWithoutExtension(job.Request.SourceName);
    if (string.IsNullOrWhiteSpace(name)) name = "surface";

    var stream = new FileStream(temp, FileMode.Open, FileAccess.Read, FileShare.Read,
        bufferSize: 1 << 16, FileOptions.DeleteOnClose | FileOptions.SequentialScan);

    return Results.File(stream,
        format == "obj" ? "text/plain" : "application/octet-stream",
        $"{name}_pltp.{format}");
});

var url = app.Configuration["urls"] ?? "http://localhost:5080";
app.Urls.Clear();
foreach (var u in url.Split(';', StringSplitOptions.RemoveEmptyEntries)) app.Urls.Add(u.Trim());

Console.WriteLine();
Console.WriteLine("  PLTP - iso-sensitivity surface extraction");
Console.WriteLine($"  open {app.Urls.First()}");
Console.WriteLine($"  samples: {samples.DataRoot ?? "not found - upload your own files"}");
Console.WriteLine();

app.Run();
return;

static ExtractionRequest ReadRequest(IFormCollection form)
{
    var req = new ExtractionRequest
    {
        ElementType = Pick(form, "elementType", "auto", "hex", "tet"),
        Format = Pick(form, "format", "auto", "alfe", "abaqus"),
        SensitivityKind = Pick(form, "sensitivityKind", "auto", "elemental", "nodal"),

        VolumeFraction = Clamp(Number(form, "volumeFraction", 0.2), 0.001, 0.999),
        Isovalue = Clamp(Number(form, "isovalue", 0.5), 0.0, 1.0),
        FilterRadius = Clamp(Number(form, "filterRadius", 3.0), 1e-9, 1e9),
        Tolerance = Clamp(Number(form, "tolerance", 0.01), 1e-9, 0.5),
        MaximumIteration = (int)Clamp(Number(form, "maximumIteration", 50), 1, 500),
        WeldTolerance = Clamp(Number(form, "weldTolerance", 1e-5), 0.0, 1.0),

        Interpolation = Flag(form, "interpolation", true),
        KeepVolume = Flag(form, "keepVolume", true),
        Normalize = Flag(form, "normalize", true),
        KeepLargestComponent = Flag(form, "keepLargestComponent", false),
    };
    return req;
}

static string Pick(IFormCollection form, string key, params string[] allowed)
{
    var v = form[key].ToString().Trim().ToLowerInvariant();
    return Array.IndexOf(allowed, v) >= 0 ? v : allowed[0];
}

static double Number(IFormCollection form, string key, double fallback)
    => double.TryParse(form[key].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
        && !double.IsNaN(v) && !double.IsInfinity(v) ? v : fallback;

static bool Flag(IFormCollection form, string key, bool fallback)
{
    var v = form[key].ToString().Trim().ToLowerInvariant();
    return v switch { "" => fallback, "true" or "1" or "on" or "yes" => true, _ => false };
}

static double Clamp(double v, double lo, double hi) => Math.Min(hi, Math.Max(lo, v));

/// <summary>Uploads keep their name for the download, but only their name.</summary>
static string Sanitise(string name, string fallback)
{
    name = Path.GetFileName(name ?? "");
    foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
    return string.IsNullOrWhiteSpace(name) ? fallback : name;
}
