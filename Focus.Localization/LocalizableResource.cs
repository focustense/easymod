using System.Collections.Generic;

namespace Focus.Localization
{
    public class LocalizableResource
    {
        public string ResourcePath { get; set; }
        public List<LocalizableSegment> Segments { get; set; }
    }
}