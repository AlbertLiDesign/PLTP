# PLTP

PLTP converts a finished BESO topology optimization result into a **watertight triangulated solid**.

It takes a finite element mesh (hexahedral or tetrahedral) together with a nodal sensitivity field and
extracts the iso-sensitivity model as an `.obj`. Unlike a plain marching-cubes isosurface, every
element contributes a **closed polyhedron of its solid part**, so the output is a manifold solid ready
for rendering, boolean operations or 3D printing rather than a bare surface.

PLTP is the post-processing companion to the [TOPX](https://github.com/AlbertLiDesign/TOPX)
optimizer: it reads TOPX's `beso.txt` model file and its `ndl_sen_<k>.txt` nodal sensitivity output
directly. Abaqus `.inp` meshes are supported as well.

---

## Table of Contents

- [Pipeline](#pipeline)
- [Requirements and Build](#requirements-and-build)
- [Usage](#usage)
- [Parameters](#parameters)
- [How Extraction Works](#how-extraction-works)
- [Input Formats](#input-formats)
- [Project Layout](#project-layout)
- [Sample Data](#sample-data)
- [Known Limitations](#known-limitations)

---

## Pipeline

```
FE mesh + nodal sensitivity field
        │
        ├─ Import.ReadHex_ALFE / ReadTet_ALFE      (TOPX beso.txt)
        │  Import.ReadHex_Abaqus / ReadTet_Abaqus  (Abaqus .inp)
        │  Import.ReadSenNum                       (one value per line)
        ↓
   HexModel / TetraModel
        │
        ├─ SortVerts()                  canonical corner ordering (hex only)
        ├─ AdjustSenNum(isovalue)       force solid / void domains
        ├─ ExtractIsoSensitivityModel() per-element closed polyhedra,
        │                               optionally bisecting the isovalue
        │                               until the target volume is met
        ↓
   one Mesh per element
        │
        ├─ Mesh.CombineMeshes
        ├─ weld vertices
        ├─ RemoveDuplicatedFaces        drops the internal shared faces
        ↓
   Export.WriteObj  →  .obj
```

---

## Requirements and Build

- **.NET 7.0**, `AnyCPU` / `x64`
- `PLTP/Dependencies/KDTree.dll` — a managed assembly, referenced locally

No native dependency and no external NuGet package, so it builds and runs on any platform.

```bash
dotnet build PLTP/PLTP.csproj -c Release
dotnet run   --project PLTP -c Release
```

---

## Usage

`Program.Main` currently calls `TestModel()`, which contains **hardcoded absolute paths**. Point it at
your own files before running:

```csharp
public static void TestModel()
{
    string mdl_path    = "../../../../../data/LetterA/beso.txt";
    string sen_path    = "../../../../../data/LetterA/elem_sen_113.txt";
    string output_path = "../../../../../data/LetterA/Smoothing.obj";

    Test.TestHex(mdl_path, sen_path,
                 volumeFraction: 0.15, isovalue: 0.05, filterRadius: 3,
                 tolerance: 0.01, maximumIteration: 50,
                 interpolation: true, keepVolume: false, output_path);
}
```

`Test.cs` provides four drivers, differing in importer and mesh-cleanup strategy:

| Driver | Mesh | Model source |
|--------|------|--------------|
| `Test.TestHex` | hexahedra | TOPX `beso.txt` |
| `Test.TestTetra` | tetrahedra | TOPX `beso.txt` |
| `Test.TestTetra_Abaqus` | tetrahedra | Abaqus `.inp` |
| `Test.ObtainSensitivityMdl` | hexahedra | Abaqus `.inp` |

A full command-line front end is written but **commented out** in `Program.cs`. Restoring it gives:

```
PLTP.exe -type <true|false> -m <mdl_path> -s <sen_path> -o <output_path>
         [-v <volumeFraction>] [-r <filterRadius>] [-t <tolerance>]
         [-iso <isovalue>] [-i <maximumIteration>] [-p <interpolation>] [-k <keepVolume>]
```

`PrintHelp()` documents every switch.

---

## Parameters

| Parameter | Default | Meaning |
|-----------|---------|---------|
| `isovalue` | 0.5 | Sensitivity threshold. A node is inside the solid when its value exceeds it. Ignored when `keepVolume` is on |
| `volumeFraction` | 0.2 | Target volume fraction, used only when `keepVolume` is on |
| `keepVolume` | true | Bisect the isovalue until the extracted volume matches `volumeFraction` |
| `tolerance` | 0.01 | Volume-fraction tolerance for that bisection |
| `maximumIteration` | 50 | Cap on bisection steps |
| `interpolation` | true | Move boundary vertices to the linearly interpolated crossing. With it off, they stay at edge midpoints and the result is blockier |
| `filterRadius` | 3.0 | Filter radius carried over from the optimization |

`keepVolume` re-extracts the entire model on every bisection step, so runtime scales with the number
of iterations actually used.

---

## How Extraction Works

For each element, the nodal sensitivities are compared against the isovalue to form an 8-bit (hex) or
4-bit (tet) case flag. That flag indexes pre-built lookup tables which return, not the cut surface,
but the **whole solid polyhedron** of the element:

| Table | Contents |
|-------|----------|
| `VertTable_Hex[flag]` | every vertex of the polyhedron — interior corners and edge crossings |
| `FaceTable_Hex[flag]` | its faces, triangles or quads |
| `ActiveTable_Hex[flag]` | which vertices are edge crossings and may therefore be moved |
| `ConnectionTable_Hex`, `EdgeFlags_Hex` | the classic marching-cubes tri table and edge mask, used to compute where the crossings land |

With `interpolation` enabled, the active vertices are then moved onto the linearly interpolated
crossing points. An element whose nodes are all inside (`flag == 255`) skips the tables and emits the
complete hexahedron.

Because each cell yields a closed solid, adjacent solid cells share coincident internal faces.
`RemoveDuplicatedFaces` deletes those pairs and leaves only the outer boundary — it is part of the
algorithm, not optional cleanup.

Solid and void domains are enforced geometrically by `AdjustSenNum`, which runs before every
extraction: nodes of solid elements are raised to `isovalue * 1.1` and nodes of void elements dropped
to `0`, so they always fall on the intended side of the threshold.

---

## Input Formats

**Model** — TOPX / ALFE `beso.txt`, comma-separated rows:

```
N,x,y,z            node
E,n0,n1,...        element (8 indices for hexahedra, 4 for tetrahedra)
SD,id              solid-domain element
VD,id              void-domain element
```

Only these prefixes are parsed; the header block is skipped. Abaqus `.inp` files are read through the
`*Node` / `*Element` blocks instead.

**Sensitivity** — one floating-point value per line, in node order (or element order when using
`CalNdlSenNums` to average onto nodes). This matches TOPX's `ndl_sen_<k>.txt` and `elem_sen_<k>.txt`.

**Output** — Wavefront `.obj`, written by `Export.WriteObj`.

---

## Project Layout

```
PLTP/
├── Program.cs               entry point; the CLI front end is present but commented out
├── Test/
│   ├── Test.cs              the four end-to-end drivers
│   └── MCCTest.cs           lookup-table experiments
├── FiniteElement/
│   ├── HexModel.cs          hexahedral extraction, volume-preserving search
│   └── TetraModel.cs        tetrahedral extraction (cases hardcoded inline)
├── Geometry/
│   ├── Mesh.cs              vertices/faces, weld, dedupe, volume, triangulation
│   ├── Hexahedron.cs        8-node cell with its nodal sensitivities
│   ├── Tetrahedron.cs       4-node cell
│   └── Quadrangle.cs, Face.cs, Box.cs, Vector.cs
├── IO/
│   ├── Import.cs            ALFE / Abaqus / MCC readers, sensitivity reader
│   └── Export.cs            .obj writer
├── Utils/
│   ├── Table.cs             the 256-case hexahedral lookup tables
│   └── Utils.cs             KD-tree search, mesh welding
└── Dependencies/KDTree.dll
```

---

## Sample Data

`data/` holds seven complete cases, each a model plus its sensitivity field:

| Case | Format |
|------|--------|
| `LetterA` | TOPX `beso.txt` + `elem_sen_113.txt` |
| `Cantilever`, `Table`, `YuLi`, `YuLi_4` | Abaqus `.inp` + `Sensitivities.txt` |
| `tetra_2`, `tetra_3` | tetrahedral Abaqus `.inp` + sensitivities |

---

## Known Limitations

- **Hexahedral tables assume a regular grid.** `Size` scales the table coordinates back to world
  space, so a non-uniform hexahedral mesh will not extract correctly.
- **There are no tetrahedral lookup tables.** `Table.cs` covers hexahedra only; `TetraModel` hardcodes
  its cases inline in `IsoSenMdl_Tetra`. The two paths must be maintained separately.
- **`SD,` / `VD,` indices are parsed as 1-based** (`int.Parse(tokens[1]) - 1`), while TOPX writes them
  from 0-based element IDs. Domains imported from TOPX would be shifted by one. No sample case in
  `data/` has a non-empty solid or void domain, so this path is untested.
- **The command line is not wired up**; `Main` runs `TestModel()` with hardcoded paths.
- **Two different welders** are in use — `MeshWeld.Weld` (tetrahedral drivers) and
  `Mesh.WeldVertices` (`TestHex`) — with tolerances from `1e-4` to `1e-10`. They are not interchangeable.
- **`Utils.cs` contains decompiled code**: `MeshWeld` is supported by `Class18` / `Class19` / `Class20`,
  machine-generated names implementing the spatial hashing used for welding.
