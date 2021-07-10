using CommandLine;

namespace Focus.Apps.EasyNpc
{
    class CommandLineOptions
    {
        [Option('d', "debug")]
        public bool DebugMode { get; set; }

        [Option('i', "force-intro")]
        public bool ForceIntro { get; set; }

        [Option('r', "report-path")]
        public string ReportPath { get; set; }

        [Option("vortex-manifest")]
        public string VortexManifest { get; set; }
    }
}