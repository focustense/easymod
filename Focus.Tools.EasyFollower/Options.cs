using CommandLine;

namespace Focus.Tools.EasyFollower
{
    class Options
    {
        [Option(
            shortName: 'b',
            longName: "backupFiles",
            HelpText =
                "Make a copy of any existing files (plugins, facegens, etc.) before overwriting. " +
                "Recommended when combining multiple followers into a single mod with -m option. " +
                "Any previous backup will be overwritten.")]
        public bool BackupFiles { get; set; }

        [Option(
            shortName: 'c',
            longName: "confirmOnExit",
            Default = true,
            HelpText =
                "Wait for key press before exiting. Defaults to true, can be disabled if running " +
                "as part of a batch script or pipeline.")]
        public bool? ConfirmOnExit { get; set; }

        [Option(
            shortName: 'd',
            longName: "dryRun",
            HelpText =
                "Performs a dry-run, i.e. does not actually create or modify any files. " +
                "Used for troubleshooting or testing new configurations.")]
        public bool DryRun { get; set; }

        [Option(
            shortName: 'f',
            longName: "filename",
            HelpText =
                "Name of the file (character) you exported. " +
                "Do not include paths or extensions; files are assumed to be in the default " +
                "RaceMenu paths, such as SKSE\\Plugins\\CharGen\\Exported.")]
        public string FileName { get; set; } = "";

        [Option(
            shortName: 'g',
            longName: "game",
            Default = "SkyrimSE",
            HelpText =
                "Edition of the game (e.g. SkyrimSE, SkyrimVR, EnderalSE, etc.) to run on.")]
        public string GameName { get; set; } = "SkyrimSE";

        [Option(
            shortName: 'm',
            longName: "mod",
            HelpText =
                "Name of the mod (i.e. ESP file, no path or extension) where the new NPC will be " +
                "added. If it does not exist, it will be created. If not specified, the plugin " +
                "name defaults to the imported file/character name.")]
        public string? ModName { get; set; }

        [Option(
            shortName: 'p',
            longName: "pause",
            HelpText = "Wait for key press before beginning operations. Used for debugging.")]
        public bool PauseOnStart { get; set; }

        [Option(
            shortName: 'v',
            longName: "verbose",
            HelpText = "Enable verbose logging. Used for debugging.")]
        public bool VerboseLogging { get; set; }
    }
}
