using System.Collections.Immutable;

namespace Focus.Graphics
{
    public class MergedSceneSource : ISceneSource
    {
        private readonly IImmutableList<ISceneSource> sources;

        public MergedSceneSource(params ISceneSource[] sources)
            : this(sources.AsEnumerable()) { }

        public MergedSceneSource(IEnumerable<ISceneSource> sources)
        {
            this.sources = sources.ToImmutableList();
        }

        public async Task<Scene> LoadAsync()
        {
            var subScenes = await Task.WhenAll(sources.Select(x => x.LoadAsync()));
            return new(
                subScenes.SelectMany(x => x.Objects).ToImmutableList(),
                subScenes.SelectMany(x => x.Lights).ToImmutableList());
        }
    }
}
