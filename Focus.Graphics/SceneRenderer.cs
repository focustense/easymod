using Silk.NET.OpenGL;
using System.Collections.Concurrent;
using System.Numerics;

namespace Focus.Graphics
{
    public class SceneRenderer : IRenderer
    {
        private readonly Func<ObjectRenderingSettings, IMeshRenderer> meshRendererFactory;
        private readonly ConcurrentQueue<IMeshRenderer> meshRenderers = new();

        private bool isDisposed = false;

        public SceneRenderer(Func<ObjectRenderingSettings, IMeshRenderer> meshRendererFactory)
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
            return Bounds3.UnionAll(meshRenderers, r => r.GetModelBounds());
        }

        public async Task LoadAsync(ISceneSource source, IScheduler scheduler)
        {
            var scene = await source.LoadAsync();
            foreach (var obj in scene.Objects)
            {
                var renderer = scheduler.Run(() =>
                {
                    var renderer = meshRendererFactory(obj.RenderingSettings);
                    renderer.LoadGeometry(obj.Mesh);
                    renderer.LoadTextures(TextureSet.Empty);
                    renderer.SetLights(scene.Lights);
                    return renderer;
                });
                meshRenderers.Enqueue(renderer);
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
            var transparentRenderers = new List<IMeshRenderer>();
            foreach (var renderer in meshRenderers)
                if (renderer.HasTransparency())
                    transparentRenderers.Add(renderer);
                else
                    renderer.Render(model, view, projection);

            // TODO: Try to sort transparent renderers by Z-order.
            // This may require applying model-view transform for each in order to get bounds.
            // Unsure what the best way is to handle overlapping geometry.
            foreach (var renderer in transparentRenderers)
                renderer.Render(model, view, projection);
        }
    }
}
