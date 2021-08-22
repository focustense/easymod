using System.Collections.Generic;

namespace Focus.Localization
{
    public class TranslationProject
    {
        public string Id { get; set; }
        public List<TranslationPackage> Packages { get; set; }
    }
}
