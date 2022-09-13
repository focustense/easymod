namespace Focus.Tools.EasyFollower
{
    public class FollowerExportData
    {
        public List<string> Abilities { get; set; } = new();
        public string Race { get; set; } = "";
        public int Sex { get; set; }
        public int SkinToneColor { get; set; }
        public float Height { get; set; }
        public List<string> Equipment { get; set; } = new();
        public List<InventoryItem> Inventory { get; set; } = new();
        public List<string> Spells { get; set; } = new();
    }

    public class InventoryItem
    {
        public string FormIdentifier { get; set; } = ""; // "Plugin.esp|xxxxxx" format
        public int Count { get; set; }
    }
}
