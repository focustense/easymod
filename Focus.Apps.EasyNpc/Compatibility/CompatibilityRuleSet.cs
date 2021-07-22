#nullable enable

using Focus.Apps.EasyNpc.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Compatibility
{
    public class CompatibilityRuleSet<T>
    {
        private readonly ILogger log;
        private readonly Func<T, string> recordNameSelector;
        private readonly List<ICompatibilityRule<T>> rules = new();

        public CompatibilityRuleSet(Func<T, string> recordNameSelector, ILogger log)
        {
            this.log = log;
            this.recordNameSelector = recordNameSelector;
        }

        public CompatibilityRuleSet<T> Add(ICompatibilityRule<T> rule)
        {
            rules.Add(rule);
            return this;
        }

        public CompatibilityRuleSet<T> AddRange(IEnumerable<ICompatibilityRule<T>> rules)
        {
            this.rules.AddRange(rules);
            return this;
        }

        public bool IsSupported(T record)
        {
            foreach (var rule in rules)
            {
                try
                {
                    var isSupported = rule.IsSupported(record);
                    if (!isSupported)
                    {
                        log.Information(
                            "Record {recordName} is blocked by rule {ruleName}.",
                            SafeGetRecordName(record), rule.Name);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    log.Error(
                        ex,
                        "Failed evaluating rule {ruleName} on record {recordName}. " +
                        "As a failsafe, this record will be considered incompatible and disabled.",
                        rule.Name, SafeGetRecordName(record));
                }
            }
            return true;
        }

        public void ReportConfiguration()
        {
            var messageBuilder = new StringBuilder()
                .AppendLine($"{AssemblyProperties.Name} is running with the following rules:");
            foreach (var rule in rules)
                messageBuilder.AppendLine($"  - {rule.Name}: {rule.Description}");
            log.Information(messageBuilder.ToString());
        }

        private string SafeGetRecordName(T record)
        {
            try
            {
                return recordNameSelector(record);
            }
            catch
            {
                return "(failed to get record name)";
            }
        }
    }
}