using System;
using System.Collections.Generic;
using System.Windows;

namespace Focus.Localization
{
    internal static class DefaultAttributes
    {
        private static readonly Dictionary<object, LocalizabilityAttribute> DefinedAttributes;

        static DefaultAttributes()
        {
            DefinedAttributes = new Dictionary<object, LocalizabilityAttribute>(32);

            var notReadable = new LocalizabilityAttribute(LocalizationCategory.None) { Readability = Readability.Unreadable };
            var notModifiable = new LocalizabilityAttribute(LocalizationCategory.None) { Modifiability = Modifiability.Unmodifiable };

            DefinedAttributes.Add(typeof(bool), notReadable);
            DefinedAttributes.Add(typeof(byte), notReadable);
            DefinedAttributes.Add(typeof(sbyte), notReadable);
            DefinedAttributes.Add(typeof(char), notReadable);
            DefinedAttributes.Add(typeof(decimal), notReadable);
            DefinedAttributes.Add(typeof(double), notReadable);
            DefinedAttributes.Add(typeof(float), notReadable);
            DefinedAttributes.Add(typeof(int), notReadable);
            DefinedAttributes.Add(typeof(uint), notReadable);
            DefinedAttributes.Add(typeof(long), notReadable);
            DefinedAttributes.Add(typeof(ulong), notReadable);
            DefinedAttributes.Add(typeof(short), notReadable);
            DefinedAttributes.Add(typeof(ushort), notReadable);
            DefinedAttributes.Add(typeof(Uri), notModifiable);
        }

        public static LocalizabilityAttribute GetDefaultAttribute(object type)
        {
            if (DefinedAttributes.TryGetValue(type, out var predefinedAttribute))
                return new LocalizabilityAttribute(predefinedAttribute.Category)
                {
                    Readability = predefinedAttribute.Readability,
                    Modifiability = predefinedAttribute.Modifiability
                };

            if (type is Type targetType && targetType.IsValueType)
            {
                // Assume that enums and other value types can't be localized.
                return new LocalizabilityAttribute(LocalizationCategory.Inherit)
                {
                    Modifiability = Modifiability.Unmodifiable
                };
            }
            
            return GetDefaultAttribute();
        }

        private static LocalizabilityAttribute GetDefaultAttribute()
        {
            return new LocalizabilityAttribute(LocalizationCategory.Inherit);
        }
    }
}