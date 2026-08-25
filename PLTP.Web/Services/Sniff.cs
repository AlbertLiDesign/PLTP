namespace PLTP.Web.Services;

/// <summary>
/// Works out which reader a model file wants.
///
/// This matters more than it looks: all four readers match on row prefixes and
/// simply produce nothing when handed the wrong shape of file, so the wrong
/// choice does not throw - it returns an empty model and the run fails much
/// later with nothing to point at. Detecting up front, and saying in the log
/// what was detected, is the difference between a clear error and a puzzle.
/// </summary>
public static class Sniff
{
    /// <summary>"abaqus" or "alfe".</summary>
    public static string Format(string path)
    {
        int scanned = 0;
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith("*Heading", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("*Node", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("*Part", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("*Element", StringComparison.OrdinalIgnoreCase))
                return "abaqus";

            if (line.StartsWith("N,", StringComparison.Ordinal) ||
                line.StartsWith("E,", StringComparison.Ordinal) ||
                line.StartsWith("%This file is created by", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("FEA Parameters", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Element Type:", StringComparison.OrdinalIgnoreCase))
                return "alfe";

            if (++scanned > 400) break;
        }
        return "alfe";
    }

    /// <summary>"hex" or "tet".</summary>
    public static string ElementType(string path, string format)
    {
        return format == "abaqus" ? FromAbaqus(path) : FromAlfe(path);
    }

    static string FromAbaqus(string path)
    {
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (!line.StartsWith("*Element", StringComparison.OrdinalIgnoreCase)) continue;

            int at = line.IndexOf("type=", StringComparison.OrdinalIgnoreCase);
            if (at < 0) continue;
            var type = line[(at + 5)..].Trim();

            // C3D10 is a ten-node tetrahedron, but ReadHex_Abaqus is the reader
            // that matches it - it takes the first eight nodes and treats them as
            // a hexahedron. Detection follows the reader rather than the element,
            // because sending it to the tetrahedral reader would produce nothing.
            if (type.StartsWith("C3D4", StringComparison.OrdinalIgnoreCase)) return "tet";
            if (type.StartsWith("C3D8", StringComparison.OrdinalIgnoreCase) ||
                type.StartsWith("C3D10", StringComparison.OrdinalIgnoreCase)) return "hex";
        }
        return "hex";
    }

    static string FromAlfe(string path)
    {
        foreach (var raw in File.ReadLines(path))
        {
            if (!raw.StartsWith("E,", StringComparison.Ordinal)) continue;
            // "E," plus four node IDs is a tetrahedron, plus eight a hexahedron.
            int commas = 0;
            for (int i = 0; i < raw.Length; i++) if (raw[i] == ',') commas++;
            return commas >= 8 ? "hex" : "tet";
        }
        return "hex";
    }
}
