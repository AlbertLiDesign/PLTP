using System.Diagnostics;
using System.Globalization;

namespace PLTP.Web.Services;

/// <summary>
/// The extraction pipeline, driven from a web request instead of from
/// <c>Test.cs</c>. Same order of operations - import, nodal field, extract,
/// combine, weld, remove the internal faces - but every step reports progress
/// and every guess the CLI made silently is decided here and written to the log.
/// </summary>
public sealed class PltpRunner
{
    public void Run(ExtractionJob job, string modelPath, string sensitivityPath)
    {
        var token = job.Cancellation.Token;
        var req = job.Request;
        var sw = Stopwatch.StartNew();
        var result = new ExtractionResult();

        job.Report("detecting", 0.01);
        var format = req.Format == "auto" ? Sniff.Format(modelPath) : req.Format;
        var element = req.ElementType == "auto" ? Sniff.ElementType(modelPath, format) : req.ElementType;
        result.Format = format;
        result.ElementType = element;
        job.Log($"Model: {format.ToUpperInvariant()} {(element == "hex" ? "hexahedra" : "tetrahedra")}"
              + (req.Format == "auto" || req.ElementType == "auto" ? "  (detected)" : ""));

        var nodes = new List<Vector>();
        var solidID = new List<int>();
        var voidID = new List<int>();

        job.Report("reading model", 0.03);
        List<Tetrahedron>? tets = null;
        List<Hexahedron>? hexes = null;

        if (element == "tet")
            tets = format == "abaqus"
                ? Import.ReadTet_Abaqus(modelPath, ref nodes, ref solidID, ref voidID)
                : Import.ReadTet_ALFE(modelPath, ref nodes, ref solidID, ref voidID);
        else
            hexes = format == "abaqus"
                ? Import.ReadHex_Abaqus(modelPath, ref nodes, ref solidID, ref voidID)
                : Import.ReadHex_ALFE(modelPath, ref nodes, ref solidID, ref voidID);

        int elementCount = tets?.Count ?? hexes!.Count;
        result.NodeCount = nodes.Count;
        result.ElementCount = elementCount;

        if (nodes.Count == 0 || elementCount == 0)
        {
            var missing = nodes.Count == 0 ? "nodes" : element == "hex" ? "hexahedra" : "tetrahedra";
            throw new InvalidOperationException(
                $"The {format.ToUpperInvariant()} reader found no {missing} in this file. The readers " +
                "match on row prefixes and fail silently, so this usually means the file is in the " +
                "other format, or holds the other element type. Set them explicitly under Overrides.");
        }

        job.Log($"{nodes.Count:N0} nodes, {elementCount:N0} elements");
        token.ThrowIfCancellationRequested();

        // The ALFE readers take SD, / VD, as 1-based while TOPX writes them from
        // 0-based element IDs, so an index can land outside the array. Dropping
        // the strays keeps a mismatched pair from taking the whole run down.
        int badSolid = solidID.RemoveAll(i => i < 0 || i >= elementCount);
        int badVoid = voidID.RemoveAll(i => i < 0 || i >= elementCount);
        if (badSolid + badVoid > 0)
            job.Log($"Dropped {badSolid + badVoid} out-of-range solid/void indices "
                  + "(the ALFE reader treats them as 1-based; TOPX writes them 0-based)", "warn");
        result.SolidDomainCount = solidID.Count;
        result.VoidDomainCount = voidID.Count;
        if (solidID.Count + voidID.Count > 0)
            job.Log($"{solidID.Count:N0} solid-domain and {voidID.Count:N0} void-domain elements");

        job.Report("reading sensitivities", 0.20);
        var field = ReadField(sensitivityPath);
        job.Log($"{field.Count:N0} sensitivity values");

        var kind = req.SensitivityKind;
        if (kind == "auto")
        {
            if (field.Count == elementCount && field.Count != nodes.Count) kind = "elemental";
            else if (field.Count == nodes.Count) kind = "nodal";
            else throw new InvalidOperationException(
                $"The sensitivity file has {field.Count:N0} values, which matches neither the " +
                $"{elementCount:N0} elements nor the {nodes.Count:N0} nodes of this model.");
            job.Log($"Field is {kind}  (detected)");
        }
        else
        {
            int expected = kind == "elemental" ? elementCount : nodes.Count;
            if (field.Count != expected)
                throw new InvalidOperationException(
                    $"A {kind} field needs {expected:N0} values; the file has {field.Count:N0}.");
        }
        result.SensitivityKind = kind;
        token.ThrowIfCancellationRequested();

        Mesh[] meshes;
        job.Report("nodal sensitivity field", 0.28);

        if (element == "tet")
        {
            var model = kind == "elemental"
                ? new TetraModel(nodes, tets!, field, elemSen: true)
                : new TetraModel(nodes, tets!);

            if (solidID.Count > 0) model.SetNonDesign(solidID);
            if (voidID.Count > 0) model.SetVoid(voidID);

            // unitise is left off: the tetrahedral path never applied it anyway,
            // and normalising here keeps hex and tet on one rule the UI can state.
            model.SetParameters(req.VolumeFraction, req.Tolerance, req.FilterRadius,
                req.MaximumIteration, req.Interpolation, req.KeepVolume, false);

            if (kind == "elemental")
            {
                job.Log($"Projecting the element field onto the nodes at rmin {Num(req.FilterRadius)}"
                      + " (a radius search - the slow step on a large mesh)");
                model.CalNdlSenNums();
            }
            else
            {
                model.SetNdlSenNums(field.ToArray());
            }

            var ndl = Normalise(model.NdlSenNum, req.Normalize, job);
            model.SetNdlSenNums(ndl);

            job.Report("extracting", 0.40);
            meshes = model.ExtractIsoSensitivityModel(req.Isovalue, OnIteration(job, req));
            result.Isovalue = model.LastIsovalue;
            result.Volume = model.LastVolume;
            result.InitialVolume = model.InitialVolume;
            result.Iterations = model.LastIterations;
        }
        else
        {
            var model = kind == "elemental"
                ? new HexModel(nodes, hexes!, field, solidID, voidID)
                : new HexModel(nodes, hexes!, solidID, voidID);

            model.SetParameters(req.VolumeFraction, req.Tolerance, req.FilterRadius,
                req.MaximumIteration, req.Interpolation, req.KeepVolume, false);

            if (kind == "elemental")
            {
                job.Log($"Projecting the element field onto the nodes at rmin {Num(req.FilterRadius)}"
                      + " (a radius search - the slow step on a large mesh)");
                model.CalNdlSenNums();
            }
            else
            {
                model.SetNdlSenNums(field.ToArray());
            }

            model.NdlSenNum = Normalise(model.NdlSenNum, req.Normalize, job);

            job.Report("ordering corners", 0.36);
            model.SortVerts();   // canonical corner order the hex lookup tables assume

            job.Report("extracting", 0.40);
            meshes = model.ExtractIsoSensitivityModel(req.Isovalue, OnIteration(job, req));
            result.Isovalue = model.LastIsovalue;
            result.Volume = model.LastVolume;
            result.InitialVolume = model.InitialVolume;
            result.Iterations = model.LastIterations;
        }

        result.VolumeFraction = result.InitialVolume > 0 ? result.Volume / result.InitialVolume : 0;
        job.Log($"Isovalue {Num(result.Isovalue)}, volume {Num(result.Volume)} "
              + $"= {result.VolumeFraction:P2} of the initial {Num(result.InitialVolume)}");

        if (req.KeepVolume && Math.Abs(result.VolumeFraction - req.VolumeFraction) > req.Tolerance)
            job.Log($"The bisection stopped short of the {req.VolumeFraction:P1} target. "
                  + "Volume is monotone in the isovalue, so this means the target is not "
                  + "attainable on this field - usually an unconverged design still much "
                  + "denser than the target.", "warn");

        token.ThrowIfCancellationRequested();

        job.Report("combining", 0.76);
        var output = Mesh.CombineMeshes(meshes);
        job.Log($"Combined: {output.Vertices.Length:N0} vertices, {output.Faces.Length:N0} faces");

        token.ThrowIfCancellationRequested();
        job.Report("welding", 0.82);
        output = MeshWeld.Weld(output, req.WeldTolerance);
        job.Log($"Welded at {Num(req.WeldTolerance)}: {output.Vertices.Length:N0} vertices");

        token.ThrowIfCancellationRequested();
        job.Report("removing internal faces", 0.88);
        // Not cleanup: every cell contributes a closed solid, so neighbouring
        // cells leave coincident internal walls. This is the step that turns the
        // pile of polyhedra into a boundary surface.
        output.RemoveDuplicatedFaces();
        job.Log($"Internal faces removed: {output.Faces.Length:N0} faces remain");

        if (req.KeepLargestComponent)
        {
            job.Report("keeping the largest piece", 0.93);
            int kept = output.KeepLargestComponent(out int dropped);
            result.DroppedFaces = dropped;
            job.Log($"Kept the largest of {kept} component(s); dropped {dropped:N0} faces");
        }

        if (output.Faces.Length == 0)
            job.Log("The surface is empty. At this isovalue nothing is above the threshold - "
                  + "try a lower isovalue, or turn the volume constraint on.", "warn");

        job.Report("packing", 0.96);
        job.Packed = MeshBinary.Pack(output, out var min, out var max, out int triangles);
        result.Min = min;
        result.Max = max;
        result.Vertices = output.Vertices.Length;
        result.Faces = output.Faces.Length;
        result.Triangles = triangles;
        result.ElapsedMs = sw.ElapsedMilliseconds;

        job.Mesh = output;
        job.Result = result;
        job.Report("done", 1.0);
        job.Log($"Done in {sw.ElapsedMilliseconds / 1000.0:0.00} s");
    }

    /// <summary>
    /// Per-trial reporting for the volume bisection, and the only place the run
    /// can be cancelled once extraction has started - each trial re-extracts the
    /// whole model, so this fires often enough to be responsive.
    /// </summary>
    static Action<int, double, double> OnIteration(ExtractionJob job, ExtractionRequest req) =>
        (iter, isovalue, fraction) =>
        {
            job.Cancellation.Token.ThrowIfCancellationRequested();
            job.Report("extracting", 0.40 + 0.34 * Math.Min(1.0, (double)iter / Math.Max(1, req.MaximumIteration)));
            job.Log($"  iteration {iter}: isovalue {Num(isovalue)} -> volume {fraction:P3}");
        };

    /// <summary>
    /// Min-max the nodal field onto [0, 1].
    ///
    /// The isovalue - and the bisection that searches for one - lives on [0, 1],
    /// so a raw field straight out of a solver (LetterA's runs around 1e-11)
    /// makes every isovalue in that range mean the same thing. The hexahedral
    /// path did this inside <c>SortVerts</c> and the tetrahedral path never did
    /// it at all; doing it here puts both on the same footing.
    /// </summary>
    static double[] Normalise(double[] field, bool on, ExtractionJob job)
    {
        if (!on) return field;

        double min = double.MaxValue, max = double.MinValue;
        for (int i = 0; i < field.Length; i++)
        {
            if (field[i] < min) min = field[i];
            if (field[i] > max) max = field[i];
        }

        if (max - min <= 0)
        {
            job.Log("The sensitivity field is constant; normalisation would divide by zero, "
                  + "so it is left as it is.", "warn");
            return field;
        }

        var scaled = new double[field.Length];
        double span = max - min;
        for (int i = 0; i < field.Length; i++) scaled[i] = (field[i] - min) / span;

        job.Log($"Normalised the nodal field from [{Num(min)}, {Num(max)}] onto [0, 1]");
        return scaled;
    }

    /// <summary>
    /// A tolerant reader for the sensitivity file: blank lines are skipped and a
    /// bad line names itself, where <c>Import.ReadSenNum</c> would throw a bare
    /// FormatException with nothing to go on.
    /// </summary>
    static List<double> ReadField(string path)
    {
        var values = new List<double>();
        int lineNo = 0;
        foreach (var raw in File.ReadLines(path))
        {
            lineNo++;
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (!double.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                throw new InvalidOperationException(
                    $"Line {lineNo} of the sensitivity file is not a number: \"{Clip(raw)}\"");
            values.Add(v);
        }
        if (values.Count == 0)
            throw new InvalidOperationException("The sensitivity file is empty.");
        return values;
    }

    static string Clip(string s) => s.Length <= 40 ? s : s[..40] + "...";

    static string Num(double v) => v.ToString("G6", CultureInfo.InvariantCulture);
}
