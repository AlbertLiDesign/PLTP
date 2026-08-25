namespace PLTP.Web.Services;

public sealed record Sample(
    string Id,
    string Name,
    string Description,
    string ModelFile,
    string SensitivityFile,
    string ElementType,
    string Format,
    double VolumeFraction,
    double FilterRadius,
    double Isovalue,
    int Elements);

/// <summary>
/// The models already in <c>data/</c>, so a fresh clone has something to extract
/// before the user has a BESO result of their own.
///
/// The filter radius is a physical length, not a count of elements, so each
/// sample carries its own - roughly two and a half times the average element
/// size of that mesh, measured from its bounding box. A shared default would be
/// meaningless across meshes whose elements differ in size.
///
/// Both are hexahedral: the tetrahedral sets were dropped from the repository
/// for their weight. The tetrahedral path is unaffected, but trying it needs a
/// model of your own through the upload tab.
///
/// Entries are filtered by whether their files are actually present, so removing
/// a data set is a matter of deleting it - nothing here has to agree with the
/// disk for the app to start.
/// </summary>
public sealed class SampleCatalog
{
    public string? DataRoot { get; }

    static readonly Sample[] Known =
    {
        new("letter-a", "Letter A",
            "Voxel model straight out of TOPX, in the ALFE text format the solver writes.",
            "LetterA/beso.txt", "LetterA/elem_sen_113.txt", "hex", "alfe", 0.15, 3.0, 0.5, 80_000),

        new("cantilever", "Cantilever",
            "The textbook cantilever, hexahedral, exported from Abaqus.",
            "Cantilever/Job-1_BESO.inp", "Cantilever/Sensitivities.txt", "hex", "abaqus", 0.30, 2.5, 0.5, 24_000),
    };

    /// <summary>
    /// The published paper, when a copy is sitting in <c>paper/</c>. Null when it
    /// is not - the citation itself is always shown, only the local link depends
    /// on the file being there.
    /// </summary>
    public string? PaperPath { get; }

    public SampleCatalog(IHostEnvironment env)
    {
        var root = FindRepositoryRoot(env.ContentRootPath);
        if (root == null) return;

        DataRoot = Path.Combine(root, "data");

        var papers = Directory.Exists(Path.Combine(root, "paper"))
            ? Directory.GetFiles(Path.Combine(root, "paper"), "*.pdf")
            : Array.Empty<string>();
        if (papers.Length > 0) PaperPath = papers[0];
    }

    /// <summary>
    /// Walks up from the content root looking for the repository, recognised by
    /// its <c>data/LetterA</c>. Run from the project directory, from the
    /// repository root, or from a published output next to it, and it finds the
    /// same place.
    /// </summary>
    static string? FindRepositoryRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
            if (Directory.Exists(Path.Combine(dir.FullName, "data", "LetterA")))
                return dir.FullName;
        return null;
    }

    public IEnumerable<Sample> Available()
    {
        if (DataRoot == null) yield break;
        foreach (var s in Known)
            if (File.Exists(Path.Combine(DataRoot, s.ModelFile)) &&
                File.Exists(Path.Combine(DataRoot, s.SensitivityFile)))
                yield return s;
    }

    public (string model, string sensitivity)? Resolve(string id)
    {
        var s = Available().FirstOrDefault(x => x.Id == id);
        if (s == null || DataRoot == null) return null;
        return (Path.Combine(DataRoot, s.ModelFile), Path.Combine(DataRoot, s.SensitivityFile));
    }

    public Sample? Find(string id) => Available().FirstOrDefault(x => x.Id == id);
}
