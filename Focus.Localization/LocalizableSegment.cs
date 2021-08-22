using System.Collections.Generic;

namespace Focus.Localization
{
    public class LocalizableSegment
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public List<LocalizableMessage> Messages { get; set; }
        public List<uint> TranslatedOrder { get; set; }
    }
}