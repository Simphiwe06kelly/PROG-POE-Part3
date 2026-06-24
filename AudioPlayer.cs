using System;
using System.IO;
using System.Media;
using System.Threading;

namespace POE_Part3
{
    /// <summary>
    /// Handles audio playback for the chatbot's voice greeting.
    /// Ported from Part 1. Plays asynchronously (background thread)
    /// so the WPF UI thread is never blocked.
    /// Gracefully falls back if the audio file is missing or corrupted.
    /// </summary>
    internal static class AudioPlayer
    {
        private const string AUDIO_FILENAME = "greeting.wav";

        /// <summary>
        /// Plays greeting.wav from the application's base directory on a
        /// background thread.  Safe to call from any thread.
        /// File must be set to 'Copy to Output Directory = Copy Always'
        /// in Visual Studio for it to be found at runtime.
        /// </summary>
        public static void PlayGreeting()
        {
            try
            {
                string audioPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    AUDIO_FILENAME);

                if (File.Exists(audioPath))
                {
                    // Run on a background thread so the UI stays responsive
                    Thread audioThread = new Thread(() =>
                    {
                        try
                        {
                            using SoundPlayer player = new SoundPlayer(audioPath);
                            player.PlaySync();
                        }
                        catch
                        {
                            // Swallow – audio is optional
                        }
                    })
                    {
                        IsBackground = true,
                        Name = "AudioGreetingThread"
                    };

                    audioThread.Start();
                }
                // If file not found, silently continue – no console output in WPF
            }
            catch
            {
                // Graceful fallback – audio is entirely optional
            }
        }
    }
}
