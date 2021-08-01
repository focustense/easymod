using Focus.Analysis.Records;
using Focus.Providers.Mutagen.Analysis;
using Moq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    public class NpcAnalyzerTests : CommonAnalyzerFacts<NpcAnalyzer, Npc, NpcAnalysis>
    {
        public NpcAnalyzerTests()
        {
            Analyzer = new NpcAnalyzer(Groups, Logger);
        }
    }
}
