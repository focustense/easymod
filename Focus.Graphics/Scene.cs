namespace Focus.Graphics
{
    public record Scene(IEnumerable<SceneObject> Objects);

    public record SceneObject(IMesh Mesh, Task<TextureSet> TexturesTask);

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
