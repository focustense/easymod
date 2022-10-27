using System.Collections.Immutable;

namespace Focus.Graphics
{
    public class SceneLighting : ISceneSource
    {
        private readonly IImmutableList<Light> lights;

        public SceneLighting(IEnumerable<Light> lights)
        {
            this.lights = lights.ToImmutableList();
        }

        public Task<Scene> LoadAsync()
        {
            return Task.FromResult(new Scene(Enumerable.Empty<SceneObject>(), lights));
        }
    }
}
