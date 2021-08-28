using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup.Localizer;

namespace Focus.Localization
{
    public class BamlLocalizabilityByReflection : BamlLocalizabilityResolver
    {
        private static readonly Dictionary<string, string> DefaultFormattingTags = new()
        {
            { typeof(Bold).FullName, "b" },
            { typeof(Hyperlink).FullName, "a" },
            { typeof(Inline).FullName, "in" },
            { typeof(Italic).FullName, "i" },
            { typeof(Underline).FullName, "u" },
        };
        private static readonly string[] WellKnownAssemblyNames =
            new string[] { "presentationcore", "presentationframework", "windowsbase" };

        private readonly Dictionary<string, Assembly> assembliesByFullName;
        private readonly Dictionary<string, Type> typesByFullName = new();

        public BamlLocalizabilityByReflection(params Assembly[] assemblies)
        {
            assembliesByFullName = WellKnownAssemblyNames
                .Select(name => Assembly.Load(GetCompatibleAssemblyName(name)))
                .Concat(assemblies)
                .ToDictionary(x => x.GetName().FullName);
        }

        public override ElementLocalizability GetElementLocalizability(string assemblyName, string className)
        {
            var localizability = new ElementLocalizability();

            var type = GetType(assemblyName, className);
            if (type != null)
                localizability.Attribute = GetLocalizabilityFromType(type);

            if (DefaultFormattingTags.TryGetValue(className, out var formattingTag))
                localizability.FormattingTag = formattingTag;

            return localizability;
        }

        public override LocalizabilityAttribute GetPropertyLocalizability(
            string assemblyName, string className, string property)
        {
            var type = GetType(assemblyName, className);
            if (type == null)
                return null;

            var result =
                GetLocalizabilityForClrProperty(property, type, out var attribute, out var clrPropertyType) ||
                GetLocalizabilityForAttachedProperty(property, type, out attribute, out var attachedPropertyType) ?
                attribute : GetLocalizabilityFromType(clrPropertyType ?? attachedPropertyType);
            return result;
        }

        public override string ResolveFormattingTagToClass(string formattingTag)
        {
            return DefaultFormattingTags.Where(x => x.Value == formattingTag).Select(x => x.Key).FirstOrDefault();
        }

        public override string ResolveAssemblyFromClass(string className)
        {
            return assembliesByFullName.Values
                .Where(x => x.GetType(className) != null)
                .Select(x => x.FullName)
                .FirstOrDefault();
        }

        private static DependencyProperty FindDependencyProperty(string propertyName, Type propertyType)
        {
            // Dependency properties should be the property name with suffix "Property".
            var dependencyPropertyName = propertyName + "Property";
            FieldInfo field = propertyType.GetField(
                dependencyPropertyName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return field?.GetValue(null) as DependencyProperty;
        }

        private static string GetCompatibleAssemblyName(string wellKnownName)
        {
            var assemblyName = typeof(BamlLocalizer).Assembly.GetName();
            assemblyName.Name = wellKnownName;
            return assemblyName.ToString();
        }

        private static bool GetLocalizabilityForAttachedProperty(
            string propertyName, Type owner, out LocalizabilityAttribute localizability, out Type propertyType)
        {
            localizability = null;
            propertyType = null;

            var attachedProperty = FindDependencyProperty(propertyName, owner);
            if (attachedProperty == null)
                return false;

            propertyType = attachedProperty.PropertyType;
            FieldInfo field = attachedProperty.OwnerType.GetField(
                attachedProperty.Name + "Property",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (field == null)
                return false;
            localizability = field.GetCustomAttribute<LocalizabilityAttribute>(true);
            return localizability != null;
        }

        private static bool GetLocalizabilityForClrProperty(
            string propertyName, Type owner, out LocalizabilityAttribute localizability, out Type propertyType)
        {
            localizability = null;
            propertyType = null;

            var propertyInfo = owner.GetProperty(propertyName);
            if (propertyInfo == null)
                return false;

            propertyType = propertyInfo.PropertyType;
            localizability = propertyInfo.GetCustomAttribute<LocalizabilityAttribute>(true);
            return localizability != null;
        }

        private static LocalizabilityAttribute GetLocalizabilityFromType(Type type)
        {
            return type?.GetCustomAttribute<LocalizabilityAttribute>(true);
        }

        private Type GetType(string assemblyName, string className)
        {
            return typesByFullName.GetOrAdd(className, () =>
                assembliesByFullName.TryGetValue(assemblyName, out var assembly) ?
                assembly.GetType(className) : null);
        }
    }
}