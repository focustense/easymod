using CommandLine;

namespace Focus.Apps.EasyNpc
{
    class CommandLineOptions
    {
        [Option('d', "debug")]
        public bool DebugMode { get; set; }

        [Option('i', "force-intro")]
        public bool ForceIntro { get; set; }
    }
}