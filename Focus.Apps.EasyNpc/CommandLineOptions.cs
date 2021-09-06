using CommandLine;

namespace Focus.Apps.EasyNpc
{
    class CommandLineOptions
    {
        [Option('d', "debug")]
        public bool DebugMode { get; set; }

        [Option('i', "force-intro")]
        public bool ForceIntro { get; set; }

        [Option('g', "game")]
        public string GameName { get; set; } = "SkyrimSE";

        [Option('p', "game-path")]
        public string? GamePath { get; set; } = null;

        [Option("mo2-exe")]
        public string? ModOrganizerExecutablePath { get; set; } = null;

        [Option('r', "report-path")]
        public string? ReportPath { get; set; }

        [Option("vortex-manifest")]
        public string? VortexManifest { get; set; }
    }
}