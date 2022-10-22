using System.Collections.Concurrent;
using System.Numerics;

namespace Focus.Graphics
{
    public class SceneRenderer : IRenderer
    {
        private readonly Func<IMeshRenderer> meshRendererFactory;
        private readonly ConcurrentBag<IMeshRenderer> meshRenderers = new();

        private bool isDisposed = false;

        public SceneRenderer(Func<IMeshRenderer> meshRendererFactory)
        {
            this.meshRendererFactory = meshRendererFactory;
        }

        public void Dispose()
        {
            isDisposed = true;
            foreach (var renderer in meshRenderers)
                renderer.Dispose();
            GC.SuppressFinalize(this);
        }

        public Bounds3 GetModelBounds()
        {
            return meshRenderers
                .Aggregate(
                    (Bounds3?)null,
                    (acc, r) => Bounds3.Union(r.GetModelBounds(), acc))
                ?? Bounds3.Default;
        }

        public async Task LoadAsync(ISceneSource source, IScheduler scheduler)
        {
            var objects = await source.LoadAsync();
            foreach (var obj in objects)
            {
                var renderer = scheduler.Run(() =>
                {
                    var renderer = meshRendererFactory();
                    renderer.LoadGeometry(obj.Mesh);
                    renderer.LoadTextures(TextureSet.Empty);
                    return renderer;
                });
                meshRenderers.Add(renderer);
                // Load textures as fire-and-forget. Having the geometry allows us to start
                // rendering, gives us scene bounds, etc. Textures can load later.
                _ = obj.TexturesTask.ContinueWith(t =>
                {
                    if (!isDisposed)
                        scheduler.Run(() => renderer.LoadTextures(t.Result));
                });
            }
        }

        public async Task LoadAsync(IEnumerable<ISceneSource> sources, IScheduler scheduler)
        {
            await Task.WhenAll(sources.Select(x => Task.Run(() => LoadAsync(x, scheduler))));
        }

        public void Render(Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
        {
            foreach (var renderer in meshRenderers)
                renderer.Render(model, view, projection);
        }
    }
}
