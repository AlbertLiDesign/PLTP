using System.Buffers.Binary;

namespace PLTP.Web.Services;

/// <summary>
/// Packs an extracted surface into the one thing the browser needs: triangles.
///
/// The mesh itself keeps its quadrangles, because the OBJ export and the face
/// count the user is shown are about the mesh as extracted. Only this copy is
/// split, and only on the way out, so nothing downstream sees a triangulated
/// mesh it did not ask for.
///
/// Layout, little-endian throughout:
///   "PLTPMSH1"   8 bytes
///   uint32       vertex count
///   uint32       triangle count
///   float32[3n]  positions
///   uint32[3m]   indices
/// Normals are left to the client - it wants both flat and smooth on a toggle,
/// and sending either doubles the payload for something a loop can do in the
/// time the transfer takes.
/// </summary>
public static class MeshBinary
{
    public static byte[] Pack(Mesh mesh, out double[] min, out double[] max, out int triangles)
    {
        var verts = mesh.Vertices;
        var faces = mesh.Faces;

        triangles = 0;
        for (int i = 0; i < faces.Length; i++)
            triangles += faces[i].Vert_ID.Length == 4 ? 2 : 1;

        int n = verts.Length;
        var buffer = new byte[8 + 4 + 4 + 12 * n + 12 * triangles];
        var span = buffer.AsSpan();

        "PLTPMSH1"u8.CopyTo(span);
        BinaryPrimitives.WriteUInt32LittleEndian(span[8..], (uint)n);
        BinaryPrimitives.WriteUInt32LittleEndian(span[12..], (uint)triangles);

        min = new[] { double.MaxValue, double.MaxValue, double.MaxValue };
        max = new[] { double.MinValue, double.MinValue, double.MinValue };

        int at = 16;
        for (int i = 0; i < n; i++)
        {
            var v = verts[i];
            BinaryPrimitives.WriteSingleLittleEndian(span[at..], (float)v.X); at += 4;
            BinaryPrimitives.WriteSingleLittleEndian(span[at..], (float)v.Y); at += 4;
            BinaryPrimitives.WriteSingleLittleEndian(span[at..], (float)v.Z); at += 4;

            if (v.X < min[0]) min[0] = v.X; if (v.X > max[0]) max[0] = v.X;
            if (v.Y < min[1]) min[1] = v.Y; if (v.Y > max[1]) max[1] = v.Y;
            if (v.Z < min[2]) min[2] = v.Z; if (v.Z > max[2]) max[2] = v.Z;
        }

        for (int i = 0; i < faces.Length; i++)
        {
            var f = faces[i].Vert_ID;
            if (f.Length == 4)
            {
                // Same split as the STL writer, so the two agree on the diagonal.
                at = Tri(span, at, f[0], f[1], f[3]);
                at = Tri(span, at, f[3], f[1], f[2]);
            }
            else
            {
                at = Tri(span, at, f[0], f[1], f[2]);
            }
        }

        if (n == 0)
        {
            min = new double[3];
            max = new double[3];
        }
        return buffer;
    }

    static int Tri(Span<byte> span, int at, int a, int b, int c)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(span[at..], (uint)a); at += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[at..], (uint)b); at += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(span[at..], (uint)c); at += 4;
        return at;
    }
}
