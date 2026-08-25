using System.Collections.Concurrent;

namespace PLTP.Web.Services;

/// <summary>
/// Holds the jobs and runs them one at a time.
///
/// One at a time on purpose: extraction is already parallel inside - the volume
/// bisection re-extracts the whole model per trial, and a million-tetrahedron
/// run peaks near 10 GB - so two concurrent runs would contend for the same
/// cores and could take the process out on memory. They queue instead.
/// </summary>
public sealed class JobStore : IDisposable
{
    readonly ConcurrentDictionary<string, ExtractionJob> jobs = new();
    readonly ConcurrentDictionary<string, string> workspaces = new();
    readonly SemaphoreSlim gate = new(1, 1);
    readonly ILogger<JobStore> log;
    readonly string root;

    /// <summary>How many finished jobs stay resident. Each keeps its surface in memory.</summary>
    const int MaxKept = 6;

    public JobStore(ILogger<JobStore> log)
    {
        this.log = log;
        // Per process: shutdown deletes this whole tree, and two instances on one
        // machine sharing it would mean one of them wiping the other's uploads.
        root = Path.Combine(Path.GetTempPath(), $"pltp-web-{Environment.ProcessId}");
        Directory.CreateDirectory(root);
    }

    public string CreateWorkspace(string jobId)
    {
        var dir = Path.Combine(root, jobId);
        Directory.CreateDirectory(dir);
        workspaces[jobId] = dir;
        return dir;
    }

    public ExtractionJob? Get(string id) => jobs.TryGetValue(id, out var j) ? j : null;

    public IEnumerable<ExtractionJob> All() => jobs.Values.OrderByDescending(j => j.Created);

    public ExtractionJob Submit(ExtractionJob job, string modelPath, string sensitivityPath)
    {
        jobs[job.Id] = job;
        job.Log($"Queued \"{job.Request.SourceName}\"");

        _ = Task.Run(async () =>
        {
            try
            {
                await gate.WaitAsync(job.Cancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                job.State = JobState.Cancelled;
                job.Report("cancelled", job.Progress);
                Cleanup(job.Id);
                return;
            }

            try
            {
                job.State = JobState.Running;
                job.Log("Started");
                new PltpRunner().Run(job, modelPath, sensitivityPath);
                job.State = JobState.Completed;
            }
            catch (OperationCanceledException)
            {
                job.State = JobState.Cancelled;
                job.Report("cancelled", job.Progress);
                job.Log("Cancelled", "warn");
            }
            catch (Exception ex)
            {
                job.State = JobState.Failed;
                job.Error = ex.Message;
                job.Report("failed", job.Progress);
                job.Log(ex.Message, "error");
                log.LogError(ex, "Extraction job {Id} failed", job.Id);
            }
            finally
            {
                gate.Release();
                Cleanup(job.Id);
                Evict();
            }
        });

        return job;
    }

    /// <summary>The uploaded copies are only needed while the readers run.</summary>
    void Cleanup(string jobId)
    {
        if (!workspaces.TryRemove(jobId, out var dir)) return;
        try { Directory.Delete(dir, true); }
        catch (Exception ex) { log.LogWarning(ex, "Could not remove {Dir}", dir); }
    }

    void Evict()
    {
        var finished = jobs.Values
            .Where(j => j.State is JobState.Completed or JobState.Failed or JobState.Cancelled)
            .OrderByDescending(j => j.Created)
            .Skip(MaxKept)
            .ToList();

        foreach (var old in finished)
        {
            if (jobs.TryRemove(old.Id, out _))
            {
                old.Mesh = null;
                old.Packed = null;
                Cleanup(old.Id);
            }
        }
    }

    public void Dispose()
    {
        gate.Dispose();
        try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { /* best effort */ }
    }
}
