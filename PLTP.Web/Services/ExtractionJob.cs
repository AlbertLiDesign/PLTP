using System.Collections.Concurrent;

namespace PLTP.Web.Services;

public enum JobState { Queued, Running, Completed, Failed, Cancelled }

/// <summary>
/// The parameters of one extraction, as they arrive from the browser. Everything
/// with an "auto" is resolved once the model is actually loaded and the true node
/// and element counts are known - guessing from the file alone is unreliable, and
/// the readers fail silently rather than throwing.
/// </summary>
public sealed class ExtractionRequest
{
    public string ElementType { get; set; } = "auto";   // auto | hex | tet
    public string Format { get; set; } = "auto";        // auto | alfe | abaqus
    public string SensitivityKind { get; set; } = "auto"; // auto | elemental | nodal

    public double VolumeFraction { get; set; } = 0.2;
    public double Isovalue { get; set; } = 0.5;
    public double FilterRadius { get; set; } = 3.0;
    public double Tolerance { get; set; } = 0.01;
    public int MaximumIteration { get; set; } = 50;

    public bool Interpolation { get; set; } = true;
    public bool KeepVolume { get; set; } = true;
    public bool Normalize { get; set; } = true;
    public bool KeepLargestComponent { get; set; }
    public double WeldTolerance { get; set; } = 1e-5;

    public string SourceName { get; set; } = "model";
}

public sealed class LogEntry
{
    public double T { get; init; }
    public string Text { get; init; } = "";
    public string Level { get; init; } = "info";
}

/// <summary>
/// What the surface came out as. Everything here is measured, not requested -
/// the achieved volume fraction in particular is usually not the target when the
/// bisection saturates, and the UI says so.
/// </summary>
public sealed class ExtractionResult
{
    public int Vertices { get; set; }
    public int Faces { get; set; }
    public int Triangles { get; set; }
    public double Volume { get; set; }
    public double InitialVolume { get; set; }
    public double VolumeFraction { get; set; }
    public double Isovalue { get; set; }
    public int Iterations { get; set; }
    public int DroppedFaces { get; set; }
    public long ElapsedMs { get; set; }

    public string ElementType { get; set; } = "";
    public string Format { get; set; } = "";
    public string SensitivityKind { get; set; } = "";
    public int NodeCount { get; set; }
    public int ElementCount { get; set; }
    public int SolidDomainCount { get; set; }
    public int VoidDomainCount { get; set; }
    public double[] Min { get; set; } = new double[3];
    public double[] Max { get; set; } = new double[3];
}

public sealed class ExtractionJob
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..12];
    public DateTimeOffset Created { get; } = DateTimeOffset.UtcNow;

    public volatile JobState State = JobState.Queued;
    public volatile string Stage = "queued";
    public double Progress;          // 0..1
    public string? Error;

    public ExtractionRequest Request { get; init; } = new();
    public ExtractionResult? Result;

    /// <summary>The extracted surface, kept so the viewer and both exporters can be served from one run.</summary>
    public Mesh? Mesh;
    public byte[]? Packed;

    readonly ConcurrentQueue<LogEntry> log = new();
    readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

    public CancellationTokenSource Cancellation { get; } = new();

    public void Log(string text, string level = "info")
        => log.Enqueue(new LogEntry { T = clock.Elapsed.TotalSeconds, Text = text, Level = level });

    public IReadOnlyList<LogEntry> Log_() => log.ToArray();

    public long ElapsedMs => clock.ElapsedMilliseconds;

    public void Report(string stage, double progress)
    {
        Stage = stage;
        Progress = Math.Clamp(progress, 0, 1);
    }
}
