using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Focus.Apps.EasyNpc.Configuration;
using Focus.Apps.EasyNpc.Profiles;
using PropertyChanged;

namespace Focus.Apps.EasyNpc.Build.Preview
{
    [AddINotifyPropertyChangedInterface]
    public class OutputViewModel
    {
        public delegate OutputViewModel Factory(Profile profile);

        public IObservable<BuildSettings> BuildSettings { get; private init; }

        public bool EnableDewiggify
        {
            get => enableDewiggify.Value;
            set => enableDewiggify.OnNext(value);
        }

        public string OutputModName
        {
            get => outputModName.Value;
            set => outputModName.OnNext(value);
        }

        private readonly BehaviorSubject<bool> enableDewiggify = new(true);
        private readonly BehaviorSubject<string> outputModName = new($"NPC Merge {DateTime.Now:yyyy-MM-dd}");
        private readonly Profile profile;

        public OutputViewModel(IObservableModSettings modSettings, Profile profile)
        {
            this.profile = profile;
            BuildSettings = Observable
                .CombineLatest(
                    modSettings.RootDirectoryObservable, outputModName, enableDewiggify, GetBuildSettings);
        }

        private BuildSettings GetBuildSettings(
            string modRootDirectory, string outputModName, bool enableDewiggify)
        {
            var outputDirectory = Path.Combine(modRootDirectory, outputModName);
            return new BuildSettings(profile, outputDirectory, outputModName)
            {
                EnableDewiggify = enableDewiggify,
            };
        }
    }
}
