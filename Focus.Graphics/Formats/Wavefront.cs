using Focus.Graphics;
using System.Numerics;

namespace Focus.Graphics.Formats
{
    public class WavefrontMesh : IMesh
    {
        public static WavefrontMesh LoadFromFile(string path)
        {
            using var fs = File.OpenRead(path);
            return LoadFromStream(fs);
        }

        public static WavefrontMesh LoadFromStream(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var file = new WavefrontMesh();
            WavefrontObject? currentObject = null;
            for (var nextLine = reader.ReadLine(); nextLine != null; nextLine = reader.ReadLine())
            {
                if (nextLine.StartsWith("#"))
                    continue;
                var parts = nextLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts[0] == "o")
                {
                    currentObject = new WavefrontObject(parts[1]);
                    file.Objects.Add(currentObject);
                }
                else if (currentObject == null)
                    continue;
                switch (parts[0])
                {
                    case "v":
                        var vertex = new Vector3(
                            float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
                        currentObject.Vertices.Add(vertex);
                        break;
                    case "vn":
                        var normal = new Vector3(
                            float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
                        currentObject.Normals.Add(normal);
                        break;
                    case "vt":
                        var uv = new Vector2(float.Parse(parts[1]), float.Parse(parts[2]));
                        currentObject.UVs.Add(uv);
                        break;
                    case "f":
                        var elements = parts.Skip(1).Select(ParseFaceElement).ToList();
                        currentObject.Faces.Add(new WavefrontFace(elements));
                        break;
                    default:
                        // Just skip unknown stuff.
                        break;
                }
            }
            return file;
        }

        public IList<WavefrontObject> Objects { get; } = new List<WavefrontObject>();

        public IEnumerable<Face> Faces => GetFaces();

        private IEnumerable<Face> GetFaces()
        {
            var points = new List<Vector3>(Objects.Sum(x => x.Vertices.Count));
            var normals = new List<Vector3>(Objects.Sum(x => x.Normals.Count));
            var uvs = new List<Vector2>(Objects.Sum(x => x.UVs.Count));
            foreach (var obj in Objects)
            {
                points.AddRange(obj.Vertices);
                normals.AddRange(obj.Normals);
                uvs.AddRange(obj.UVs);
            }
            return Objects
                .SelectMany(o => o.Faces)
                .Select(f => new Face(f.Elements.Select(FaceElementToVertex)))
                // Streaming the result could be unpredictable due to the dependency on list
                // variable captures above. Safer to materialize immediately.
                .ToList();

            Vertex FaceElementToVertex(WavefrontFaceElement element)
            {
                return new Vertex(
                    points[(int)element.VertexIndex],
                    normals[(int)element.NormalIndex],
                    uvs[(int)element.UVIndex]);
            }
        }

        private static WavefrontFaceElement ParseFaceElement(string s)
        {
            var parts = s.Split('/');
            return new WavefrontFaceElement(
                uint.Parse(parts[0]) - 1, uint.Parse(parts[1]) - 1, uint.Parse(parts[2]) - 1);
        }
    }

    public record WavefrontObject(
        string name, IList<Vector3> Vertices, IList<Vector3> Normals, IList<Vector2> UVs,
        IList<WavefrontFace> Faces)
    {
        public WavefrontObject(string name) : this(
            name, new List<Vector3>(), new List<Vector3>(), new List<Vector2>(),
            new List<WavefrontFace>())
        { }
    }

    public record WavefrontFace(IList<WavefrontFaceElement> Elements) { }

    public record WavefrontFaceElement(uint VertexIndex, uint UVIndex, uint NormalIndex);
}
