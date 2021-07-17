using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Focus.Audio
{
    public class Player : IDisposable
    {
        enum AudioFileType { Unknown, Music, Voice };

        private readonly XAudio2 device;
        private readonly MasteringVoice master;

        private bool disposed = false;
        private CancellationTokenSource playbackCancellationTokenSource;

        public Player()
        {
            device = new();
            master = new(device);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Task Play(string path)
        {
            var extension = Path.GetExtension(path);
            var type = GetFileType(extension);
            using var fs = File.OpenRead(path);
            if (type != AudioFileType.Voice)
                return Play(fs);

            // FUZ files (actor voices)
            using var reader = new BinaryReader(fs);
            reader.ReadInt32(); // FUZE magic
            reader.ReadInt32(); // Version
            var lipDataSize = reader.ReadInt32();
            fs.Seek(lipDataSize, SeekOrigin.Current);
            var ms = new MemoryStream();
            fs.CopyTo(ms);
            ms.Position = 0;
            return Play(ms);
        }

        public async Task Play(Stream stream)
        {
            if (playbackCancellationTokenSource != null)
                playbackCancellationTokenSource.Cancel();
            using var cancellationTokenSource = new CancellationTokenSource();
            playbackCancellationTokenSource = cancellationTokenSource;
            using var soundStream = new SoundStream(stream);
            var buffer = new AudioBuffer(soundStream) { Flags = BufferFlags.EndOfStream };
            using var voice = new SourceVoice(device, soundStream.Format);
            voice.SubmitSourceBuffer(buffer, soundStream.DecodedPacketsInfo);
            voice.Start();
            await WaitForVoice(voice, playbackCancellationTokenSource.Token);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
            {
                master.Dispose();
                device.Dispose();
            }
            disposed = true;
        }

        private static AudioFileType GetFileType(string extension)
        {
            switch (extension.ToLower())
            {
                case ".xwm":
                    return AudioFileType.Music;
                case ".fuz":
                    return AudioFileType.Voice;
                default:
                    return AudioFileType.Unknown;
            }
        }

        private static Task WaitForVoice(SourceVoice voice, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource();
            cancellationToken.Register(() => tcs.SetResult());
            voice.VoiceError += _ => tcs.SetResult();
            voice.StreamEnd += () => tcs.SetResult();
            return tcs.Task;
        }
    }
}