using System.Drawing;

namespace Focus.Graphics
{
    public record Scene(IEnumerable<SceneObject> Objects);

    public record SceneObject(IMesh Mesh, Task<TextureSet> TexturesTask, ObjectRenderingSettings RenderingSettings)
    {
        public SceneObject(IMesh mesh, Task<TextureSet> texturesTask)
            : this(mesh, texturesTask, new ObjectRenderingSettings()) { }
    }

    public enum NormalSpace
    {
        TangentSpace = 0,
        ObjectSpace = 1,
    }

    public enum SpecularSource
    {
        None = 0,
        NormalMapAlpha = 1,
        SpecularMap = 2,
    }

    public class ObjectRenderingSettings
    {
        public Color AmbientLightingColor { get; set; } = Color.White;
        public float AmbientLightingStrength { get; set; } = 0.4f;
        public Color DiffuseLightingColor { get; set; } = Color.White;
        public float DiffuseLightingStrength { get; set; } = 1.0f;
        public NormalSpace NormalSpace { get; set; } = NormalSpace.TangentSpace;
        public float Shininess { get; set; } = 32;
        public Color SpecularLightingColor { get; set; } = Color.White;
        public float SpecularLightingStrength { get; set; } = 1.0f;
        public SpecularSource SpecularSource { get; set; } = SpecularSource.None;
    }

    public interface ISceneSource
    {
        IEnumerable<SceneObject> Load()
        {
            // We should get the same outcome with LoadAsync().Result, but this way makes the
            // semantics clearer.
            var loadTask = LoadAsync();
            loadTask.Wait();
            return loadTask.Result;
        }

        Task<IEnumerable<SceneObject>> LoadAsync();
    }
}
