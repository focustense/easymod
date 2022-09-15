namespace Focus.Tools.EasyFollower
{
    static class Files
    {
        public static void Backup(string fileName)
        {
            if (!File.Exists(fileName))
                return;
            File.Copy(
                fileName,
                Path.ChangeExtension(fileName, $"{Path.GetExtension(fileName)}.backup"),
                /* overwrite */ true);
        }
    }
}
