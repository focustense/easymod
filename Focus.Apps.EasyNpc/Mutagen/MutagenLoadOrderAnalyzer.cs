using Focus.Analysis.Execution;
using Focus.Analysis.Records;
using Focus.Providers.Mutagen.Analysis;
using Serilog;

namespace Focus.Apps.EasyNpc.Mutagen
{
    public class MutagenLoadOrderAnalyzer : LoadOrderAnalyzer
    {
        private readonly IGroupCache groupCache;
        private readonly bool purgeOnComplete;

        public MutagenLoadOrderAnalyzer(IGroupCache groupCache, ILogger log)
            : this(groupCache, log, true) { }

        public MutagenLoadOrderAnalyzer(IGroupCache groupCache, ILogger log, bool purgeOnComplete = true)
            : base(new RecordScanner(groupCache), log)
        {
            this.groupCache = groupCache;
            this.purgeOnComplete = purgeOnComplete;
        }

        protected override void Configure(AnalysisRunner runner)
        {
            runner
                .Configure(RecordType.Npc, new NpcAnalyzer(groupCache, Log))
                .Configure(RecordType.HeadPart, new HeadPartAnalyzer(groupCache));
        }

        protected override void OnCompleted(LoadOrderAnalysis analysis)
        {
            base.OnCompleted(analysis);
            if (purgeOnComplete)
                groupCache.Purge();
        }
    }
}
