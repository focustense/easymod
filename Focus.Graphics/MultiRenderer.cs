using System.Collections.Immutable;
using System.Numerics;

namespace Focus.Graphics
{
    public class MultiRenderer : IRenderer
    {
        private readonly IImmutableList<IRenderer> renderers;

        public MultiRenderer(params IRenderer[] renderers)
        {
            this.renderers = renderers.ToImmutableList();
        }

        public MultiRenderer(IEnumerable<IRenderer> renderers)
        {
            this.renderers = renderers.ToImmutableList();
        }

        public void Dispose()
        {
            foreach (var renderer in renderers)
                renderer.Dispose();
            GC.SuppressFinalize(this);
        }

        public Bounds3 GetModelBounds()
        {
            return Bounds3.UnionAll(renderers, r => r.GetModelBounds());
        }

        public void Render(Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
        {
            foreach (var renderer in renderers)
                renderer.Render(model, view, projection);
        }
    }
}
