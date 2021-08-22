using System.Collections.Generic;

namespace Focus.Localization
{
    public class TranslationPackage
    {
        public string ProjectId { get; set; }
        public string LanguageCode { get; set; }
        public List<LocalizableResource> Resources { get; set; }
    }
}