using CommandLine;
using Focus.Tools.EasyFollower;
using Serilog;
using Serilog.Core;
using Serilog.Events;

var loggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
var log = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(loggingLevelSwitch)
    .WriteTo.Console()
    .CreateLogger();
int statusCode = 0;
Parser.Default
    .ParseArguments<Options>(args)
    .WithParsed(options =>
    {
        if (options.VerboseLogging)
        {
            loggingLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            log.Verbose("Debug logging enabled.");
        }
        if (options.DryRun)
            log.Information("Dry run enabled; no files will be written.");
        Console.WriteLine();
        if (string.IsNullOrWhiteSpace(options.FileName))
        {
            Console.Write("Name of file (character) to convert: ");
            options.FileName = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(options.FileName))
            {
                log.Error("No filename specified; aborting.");
                statusCode = -1;
                return;
            }
        }
        if (options.PauseOnStart)
        {
            Console.WriteLine("Press any key...");
            Console.ReadKey();
            Console.WriteLine();
        }
        try
        {
            using var container = AppContainer.Create(options.GameName, log);
            var outputModName =
                !string.IsNullOrWhiteSpace(options.ModName) ? options.ModName : options.FileName;
            if (!Path.HasExtension(outputModName))
                outputModName = Path.ChangeExtension(outputModName, "esp");
            var nifEditor = new NifEditor(container.FileProvider, log);
            var patcher = new Patcher(container.Environment, nifEditor, log);
            var converter =
                new FollowerConverter(patcher, container.Environment.DataFolderPath, log);
            var outputMode = options.DryRun
                ? OutputMode.DryRun
                : options.BackupFiles
                    ? OutputMode.NormalWithBackup
                    : OutputMode.Normal;
            if (!converter.Convert(options.FileName, outputModName, outputMode))
                statusCode = -2;
        } catch (Exception ex)
        {
            log.Error(ex, "Unable to complete the conversion.");
            statusCode = -1;
        }
        if (statusCode >= 0)
            Console.WriteLine("All done!");
        if (options.ConfirmOnExit != false)
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    })
    .WithNotParsed(errors =>
    {
        var hasMeaningfulErrors = errors
            .Where(e => e.StopsProcessing && e.Tag != ErrorType.HelpRequestedError)
            .Any();
        if (hasMeaningfulErrors)
        {
            log.Error("Invalid options specified. Run with --help to see available options.");
            statusCode = -1;
        }
    });
return statusCode;