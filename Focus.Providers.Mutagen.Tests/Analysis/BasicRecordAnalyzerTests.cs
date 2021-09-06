using Focus.Analysis.Records;
using Focus.Providers.Mutagen.Analysis;
using Mutagen.Bethesda.Skyrim;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    public class BasicRecordAnalyzerTests : CommonAnalyzerFacts<BasicRecordAnalyzer, Container, BasicRecordAnalysis>
    {
        public BasicRecordAnalyzerTests()
        {
            Analyzer = new BasicRecordAnalyzer(Groups, RecordType.Container, ReferenceChecker);
        }
    }
}
