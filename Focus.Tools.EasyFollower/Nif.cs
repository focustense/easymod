using nifly;

namespace Focus.Tools.EasyFollower
{
    class Nif : IDisposable
    {
        public static Nif LoadFromFile(string fileName)
        {
            var file = new NifFile();
            var loadResult = file.Load(fileName);
            if (loadResult != 0)
                throw new FileFormatException(
                    string.Format("Couldn't load NIF: {0} ({1})", fileName, loadResult));
            return new Nif(file);
        }

        public NifNode RootNode => new(file, file.GetRootNode());

        private readonly NifFile file;

        public Nif(IEnumerable<byte> data)
            : this(new NifFile(new vectoruchar(data)))
        {
        }

        private Nif(NifFile file)
        {
            this.file = file;
        }

        public void Dispose()
        {
            file.Dispose();
        }

        public NifNode? FindNode(string name)
        {
            var nodeNames = file.GetNodes().Select(x => x.name.get()).ToList();
            var node = file.GetNodes().FirstOrDefault(x => x.name.get() == name);
            return node != null ? new NifNode(file, node) : null;
        }

        public void SaveToFile(string fileName)
        {
            file.Save(fileName);
        }
    }

    class NifNode
    {
        public string Name => node.name.get();

        protected internal NifFile File => file;

        private readonly NifFile file;
        private readonly NiNode node;

        public NifNode(NifFile file, NiNode node)
        {
            this.file = file;
            this.node = node;
        }

        public void Delete()
        {
            file.DeleteNode(Name);
        }

        public IEnumerable<NifNode> GetChildren()
        {
            return GetChildNodes().Select(x => new NifNode(file, x));
        }

        public IEnumerable<NifShape> GetShapes()
        {
            return GetChildObjects().OfType<NiShape>().Select(x => new NifShape(file, x));
        }

        public string? GetExtraStringData(string name)
        {
            var header = file.GetHeader();
            var extraDataIds =
                node.extraDataRefs.GetRefs().Select(x => x.index)
                ?? Enumerable.Empty<uint>();
            return extraDataIds
                .Select(id => header.GetBlockById(id))
                .OfType<NiStringExtraData>()
                .Where(x => x.name.get() == name)
                .Select(x => header.GetStringById(x.stringData.GetIndex()))
                .FirstOrDefault();
        }

        private IEnumerable<NiNode> GetChildNodes()
        {
            return GetChildObjects().OfType<NiNode>();
        }

        private IEnumerable<NiObject> GetChildObjects()
        {
            var header = file.GetHeader();
            var refs = new setNiRef();
            node.GetChildRefs(refs);
            return refs.Select(x => header.GetBlockById(x.index));
        }
    }

    class NifShape
    {
        public string Name => shape.name.get();

        private readonly NifFile file;
        private readonly NiShape shape;

        public NifShape(NifFile file, NiShape shape)
        {
            this.file = file;
            this.shape = shape;
        }

        public void CopyTo(NifNode parent)
        {
            parent.File.CloneShape(shape, shape.name.get(), file);
        }

        public void Delete()
        {
            file.DeleteShape(shape);
        }
    }
}
