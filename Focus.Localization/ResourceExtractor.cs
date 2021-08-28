using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Web;
using System.Windows;
using System.Windows.Markup.Localizer;
using System.Xml.Linq;

namespace Focus.Localization
{
    public class ResourceExtractor
    {
        public IEnumerable<LocalizableResource> Extract(Assembly assembly, string commentsFilePath = null)
        {
            var placeholderNames = ReadPlaceholderNames(commentsFilePath);
            return GetBamlResources(assembly)
                .Where(x => x.PropertyName == "$Content")
                .Select(info => (ParseSegmentInfo(info.Uid), info))
                .GroupBy(x => new { SegmentId = x.Item1.Id, ResourcePath = x.info.ResourceName })
                .GroupBy(x => x.Key.ResourcePath)
                .Select(g => new LocalizableResource
                {
                    ResourcePath = g.Key,
                    Segments = g.Select(x => new LocalizableSegment
                    {
                        Id = x.Key.SegmentId,
                        Messages = x.Select(y => new LocalizableMessage
                        {
                            Id = y.info.Uid,
                            Source = HttpUtility.HtmlDecode(y.info.Content),
                            SourceIndex = y.Item1.Index,
                        }).ToList()
                    }).Tap(x => TransformSegment(x, g.Key, placeholderNames)).ToList()
                });
        }

        private static string ExtractContentPlaceholderName(string comments)
        {
            var contentStartPosition = comments.IndexOf("$Content");
            if (contentStartPosition < 0)
                return null;
            var contentEndPosition = comments.IndexOf(")", contentStartPosition);
            if (contentEndPosition < 0)
                return null;
            return comments[(contentStartPosition + 10)..contentEndPosition]
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split('='))
                .Where(a => a.Length == 2 && a[0] == "PH")
                .Select(a => a[1])
                .FirstOrDefault();
        }

        private static IEnumerable<BamlResourceInfo> GetBamlResources(Assembly assembly)
        {
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (!resourceName.EndsWith(".resources"))
                    continue;
                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new ResourceReader(stream);
                foreach (DictionaryEntry entry in reader)
                {
                    if (entry.Value is Stream entryStream)
                    {
                        var resolver = new BamlLocalizabilityByReflection(assembly);
                        var localizer = new BamlLocalizer(entryStream, resolver);
                        var resources = localizer.ExtractResources();
                        foreach (var res in resources)
                        {
                            var key = res.Key as BamlLocalizableResourceKey;
                            var value = res.Value as BamlLocalizableResource;
                            if (IsLocalizable(value))
                                yield return new(resourceName, (string)entry.Key, key, value);
                        }
                    }
                }
            }
        }

        private static bool IsLocalizable(BamlLocalizableResource resource)
        {
            return
                resource.Readable &&
                resource.Modifiable &&
                resource.Category != LocalizationCategory.Font &&
                resource.Category != LocalizationCategory.Hyperlink &&
                resource.Category != LocalizationCategory.Ignore &&
                resource.Category != LocalizationCategory.NeverLocalize &&
                resource.Category != LocalizationCategory.XmlData &&
                !string.IsNullOrWhiteSpace(resource.Content);
        }

        private static (string Id, uint Index) ParseSegmentInfo(string uid)
        {
            var separatorPosition = uid.LastIndexOf('.');
            if (separatorPosition < 0)
                return (uid, 0);
            var lastToken = uid[(separatorPosition + 1)..];
            return uint.TryParse(lastToken, out var index) ? (uid[0..separatorPosition], index) : (uid, 0);
        }

        private static IReadOnlyDictionary<string, string> ReadPlaceholderNames(string commentsFilePath)
        {
            if (string.IsNullOrEmpty(commentsFilePath) || !File.Exists(commentsFilePath))
                return new Dictionary<string, string>();
            var items =
                from file in XDocument.Load(commentsFilePath).Element("LocalizableAssembly").Elements("LocalizableFile")
                let fileName = file.Attribute("Name").Value.Replace('\\', '/').ToLower() + ".baml"
                from directive in file.Elements("LocalizationDirectives")
                let uid = directive.Attribute("Uid").Value
                let comments = directive.Attribute("Comments").Value
                let placeholderName = ExtractContentPlaceholderName(comments)
                select new { fileName, uid, placeholderName };
            return items.ToDictionary(x => $"{x.fileName}:{x.uid}", x => x.placeholderName);
        }

        private static void TransformSegment(
            LocalizableSegment segment, string resourcePath, IReadOnlyDictionary<string, string> placeholderNames)
        {
            if (segment.Messages.Count == 1)
            {
                segment.SourceFormat = "{1}";
                return;
            }
            // The first message should be the "index" specifying the order of other segments and any inline content.
            var indexFormat = segment.Messages[0].Source;
            for (int i = 1; i < segment.Messages.Count; i++)
            {
                var message = segment.Messages[i];
                indexFormat = indexFormat.Replace($"#{message.Id};", "{" + i + "}");
            }
            int nextRefStartIndex = indexFormat.IndexOf('#');
            while (nextRefStartIndex >= 0)
            {
                var nextRefEndIndex = indexFormat.IndexOf(';', nextRefStartIndex);
                if (nextRefEndIndex < 0)
                    break;
                var originalRefName = indexFormat[(nextRefStartIndex + 1)..nextRefEndIndex];
                var refName = originalRefName;
                if (placeholderNames.TryGetValue($"{resourcePath}:{refName}", out var placeholderName))
                    refName = $"[[placeholderName]]";
                else if (refName.StartsWith(segment.Id))
                    refName = "#" + originalRefName[(segment.Id.Length + 1)..] + ";";
                if (refName != originalRefName)
                    indexFormat = indexFormat[0..nextRefStartIndex] + refName + indexFormat[(nextRefEndIndex + 1)..];
                nextRefStartIndex = indexFormat.IndexOf('#', nextRefStartIndex + 1);
            }
            segment.Messages.RemoveAt(0);
            segment.SourceFormat = indexFormat;
        }

        class BamlResourceInfo
        {
            public string Content { get; }
            public string PropertyName { get; }
            public string ResourceName { get; }
            public string ResourceSetName { get; }
            public string Uid { get; }

            public BamlResourceInfo(
                string resourceSetName, string resourceName, BamlLocalizableResourceKey key,
                BamlLocalizableResource resource)
            {
                ResourceSetName = resourceSetName;
                ResourceName = resourceName;
                Uid = key.Uid;
                PropertyName = key.PropertyName;
                Content = resource.Content;
            }
        }
    }
}