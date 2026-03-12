using FoodStreetApp.Models;
using Plugin.Maui.Audio;

namespace FoodStreetApp.Services
{
    public class NarrationService : INarrationService
    {
        private readonly IAudioManager _audioManager;
        private IAudioPlayer? _currentPlayer;
        private volatile bool _isSpeaking = false;
        private string _currentLanguage = "vi";

        // CancellationToken used to cancel an in-progress SpeakAsync call.
        // Cancelled in StopNarrationAsync() instead of calling SpeakAsync(" ").
        private CancellationTokenSource? _ttsCancelToken;

        // Locales cached once at startup so GetLocaleFromCache() is always non-blocking.
        private IEnumerable<Locale>? _cachedLocales;

        public NarrationService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
            LoadLanguagePreference();
            // Cache TTS locales in the background — avoids blocking the UI thread later.
            _ = CacheLocalesAsync();
        }

        private void LoadLanguagePreference()
        {
            _currentLanguage = Preferences.Get("app_language", "vi");
            System.Diagnostics.Debug.WriteLine($"[NARRATION] Loaded language preference: '{_currentLanguage}'");
        }

        /// <summary>
        /// Pre-fetches the list of available TTS locales in the background.
        /// Must be awaited rather than blocking (.Result) to avoid UI-thread deadlocks.
        /// </summary>
        private async Task CacheLocalesAsync()
        {
            try
            {
                _cachedLocales = await TextToSpeech.Default.GetLocalesAsync();
                System.Diagnostics.Debug.WriteLine($"[NARRATION] Cached {_cachedLocales?.Count() ?? 0} TTS locales");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NARRATION] Failed to cache locales: {ex.Message}");
                _cachedLocales = null;
            }
        }

        public void SetLanguage(string languageCode)
        {
            _currentLanguage = languageCode;
            Preferences.Set("app_language", languageCode);
            System.Diagnostics.Debug.WriteLine($"[NARRATION] Language changed to: '{languageCode}'");
        }

        /// <summary>
        /// Play narration for a POI.
        /// Priority 1: TTS (when UseTts = true and TTS text exists).
        /// Priority 2: Audio file (poi.AudioFile).
        /// Fallback:   TTS with the generic template from GetTtsText().
        /// Safe to call from any thread (UI or background).
        /// </summary>
        public async Task PlayNarrationAsync(POI poi)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[NARRATION] ▶ PlayNarrationAsync: '{poi.Name}'");
                System.Diagnostics.Debug.WriteLine($"[NARRATION]   Language: '{_currentLanguage}' | UseTts: {poi.UseTts} | AudioFile: '{poi.AudioFile}'");

                // Stop whatever is currently playing before starting new narration.
                await StopNarrationAsync();
                _isSpeaking = true;

                // --- Priority 1: TTS ---
                if (poi.UseTts)
                {
                    var ttsText = poi.GetTtsText(_currentLanguage);
                    System.Diagnostics.Debug.WriteLine($"[NARRATION]   TTS text: '{ttsText}'");

                    if (!string.IsNullOrWhiteSpace(ttsText))
                    {
                        await SpeakWithTtsAsync(ttsText);
                        _isSpeaking = false;
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"[NARRATION]   TTS text is empty for '{_currentLanguage}' — trying audio file or fallback");
                }

                // --- Priority 2: Audio file ---
                if (!string.IsNullOrWhiteSpace(poi.AudioFile))
                {
                    System.Diagnostics.Debug.WriteLine($"[NARRATION]   Loading audio file: '{poi.AudioFile}'");
                    try
                    {
                        var stream = await FileSystem.OpenAppPackageFileAsync(poi.AudioFile);
                        _currentPlayer = _audioManager.CreatePlayer(stream);
                        _currentPlayer.Play();
                        System.Diagnostics.Debug.WriteLine($"[NARRATION]   ✅ Audio playback started: '{poi.AudioFile}'");
                        _isSpeaking = false;
                        return;
                    }
                    catch (Exception audioEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[NARRATION]   ⚠️ Audio file failed ({audioEx.Message}) — falling back to TTS");
                    }
                }

                // --- Fallback: TTS generic text (covers UseTts=false + no audio file) ---
                var fallbackText = poi.GetTtsText(_currentLanguage);
                if (!string.IsNullOrWhiteSpace(fallbackText))
                {
                    System.Diagnostics.Debug.WriteLine($"[NARRATION]   Using fallback TTS: '{fallbackText}'");
                    await SpeakWithTtsAsync(fallbackText);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[NARRATION]   ❌ No narration content available for '{poi.Name}'");
                }

                _isSpeaking = false;
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"[NARRATION]   Narration cancelled for '{poi.Name}'");
                _isSpeaking = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NARRATION]   ❌ ERROR in PlayNarrationAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[NARRATION]   Stack: {ex.StackTrace}");
                _isSpeaking = false;
            }
        }

        /// <summary>
        /// Speak text using TTS with a fresh CancellationToken.
        /// Using the cached locale avoids any blocking .GetLocalesAsync().Result call.
        /// </summary>
        private async Task SpeakWithTtsAsync(string text)
        {
            _ttsCancelToken = new CancellationTokenSource();
            var locale = GetLocaleFromCache(_currentLanguage);

            System.Diagnostics.Debug.WriteLine(
                $"[NARRATION]   → TextToSpeech.SpeakAsync | locale: '{locale?.Language ?? "system default"}'");

            await TextToSpeech.Default.SpeakAsync(
                text,
                new SpeechOptions { Locale = locale },
                _ttsCancelToken.Token);

            System.Diagnostics.Debug.WriteLine("[NARRATION]   ✅ SpeakAsync completed");
        }

        /// <summary>
        /// Stop any active narration.
        /// Cancels the CancellationToken passed to SpeakAsync — the correct way to
        /// interrupt TTS in .NET MAUI (calling SpeakAsync(" ") instead just queues
        /// another utterance and blocks the real narration).
        /// </summary>
        public async Task StopNarrationAsync()
        {
            try
            {
                // Cancel in-progress TTS via its token.
                if (_ttsCancelToken != null && !_ttsCancelToken.IsCancellationRequested)
                {
                    _ttsCancelToken.Cancel();
                    System.Diagnostics.Debug.WriteLine("[NARRATION] TTS cancelled via token");
                }
                _ttsCancelToken = null;

                // Stop audio player.
                if (_currentPlayer != null)
                {
                    if (_currentPlayer.IsPlaying)
                        _currentPlayer.Stop();
                    _currentPlayer.Dispose();
                    _currentPlayer = null;
                    System.Diagnostics.Debug.WriteLine("[NARRATION] Audio player stopped");
                }

                _isSpeaking = false;

                // Brief delay to let the TTS engine settle before a new SpeakAsync call.
                await Task.Delay(150);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NARRATION] StopNarration error: {ex.Message}");
            }
        }

        public Task<bool> IsSpeakingAsync()
        {
            return Task.FromResult(_isSpeaking || _currentPlayer?.IsPlaying == true);
        }

        /// <summary>
        /// Returns the best available locale for the given language code using the
        /// pre-cached locale list. Always non-blocking. Returns null if not found
        /// (TextToSpeech will then use the system default voice).
        /// </summary>
        private Locale? GetLocaleFromCache(string languageCode)
        {
            if (_cachedLocales == null)
            {
                System.Diagnostics.Debug.WriteLine("[NARRATION]   Locale cache empty — using system default");
                return null;
            }

            var locale = _cachedLocales.FirstOrDefault(l =>
                l.Language.StartsWith(languageCode, StringComparison.OrdinalIgnoreCase));

            if (locale != null)
                System.Diagnostics.Debug.WriteLine($"[NARRATION]   Locale found: {locale.Language}-{locale.Country}");
            else
                System.Diagnostics.Debug.WriteLine($"[NARRATION]   No locale for '{languageCode}' — using system default");

            return locale;
        }
    }
}
