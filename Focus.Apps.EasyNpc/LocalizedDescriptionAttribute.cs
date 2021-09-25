using System;
using System.ComponentModel;
using System.Resources;

namespace Focus.Apps.EasyNpc
{
    // TODO: Move to l10n library when doing the rest of localization.
    [AttributeUsage(AttributeTargets.All)]
    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        public string Key { get; private init; }
        public Type Resources { get; private init; }

        private readonly ResourceManager resourceManager;

        public LocalizedDescriptionAttribute(Type resources, string key)
        {
            Resources = resources;
            Key = key;

            resourceManager = new(Resources);
        }

        public override string Description => GetDescription();

        private string GetDescription()
        {
            var localizedDescription = resourceManager.GetString(Key);
            return !string.IsNullOrEmpty(localizedDescription) ?
                localizedDescription : $"{Resources.GetType().Name}:{Key}";
        }
    }
}
