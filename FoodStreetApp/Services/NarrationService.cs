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

        private CancellationTokenSource? _ttsCancelToken;
        private IEnumerable<Locale>? _cachedLocales;

        private readonly Queue<POI> _narrationQueue = new();
        private bool _isProcessingQueue = false;
        
        private TaskCompletionSource<bool>? _audioCompletionSource;
        public event EventHandler? NarrationFinished;

        private string? _lastTtsText = null;
        private Locale? _lastTtsLocale = null;
        private bool _isTtsPaused = false;
        
        private readonly ITrackingService _trackingService;
        private DateTime? _playStartTime = null;
        private int? _currentPlayingPoiId = null;

        public NarrationService(IAudioManager audioManager, ITrackingService trackingService)
        {
            _audioManager = audioManager;
            _trackingService = trackingService;
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
        public async Task PlayNarrationAsync(POI poi, bool isManual = false)
        {
            if (isManual)
            {
                _narrationQueue.Clear();
                await StopNarrationAsync();
                await ProcessSingleNarrationAsync(poi);
            }
            else
            {
                _narrationQueue.Enqueue(poi);
                if (!_isProcessingQueue)
                {
                    _ = ProcessQueueAsync();
                }
            }
        }

        private async Task ProcessQueueAsync()
        {
            _isProcessingQueue = true;
            while (_narrationQueue.Count > 0)
            {
                var poi = _narrationQueue.Dequeue();
                await ProcessSingleNarrationAsync(poi);
            }
            _isProcessingQueue = false;
        }

        private async Task ProcessSingleNarrationAsync(POI poi)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[NARRATION] ▶ ProcessSingleNarrationAsync: '{poi.Name}'");
                System.Diagnostics.Debug.WriteLine($"[NARRATION]   Language: '{_currentLanguage}' | UseTts: {poi.UseTts} | AudioFile: '{poi.AudioFile}'");

                // Stop whatever is currently playing before starting new narration.
                await StopNarrationAsync();
                _isSpeaking = true;
                _playStartTime = DateTime.UtcNow;
                _currentPlayingPoiId = poi.Id;

                // --- Priority 1: TTS ---
                if (poi.UseTts)
                {
                    // Check if we need to fall back to English text because the device lacks the requested TTS voice
                    var activeLanguage = _currentLanguage;
                    var locale = GetLocaleFromCache(_currentLanguage);

                    if (locale != null && !locale.Language.StartsWith(_currentLanguage, StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine($"[NARRATION]   Device lacks TTS voice for '{_currentLanguage}'. Falling back to English text.");
                        activeLanguage = "en";
                    }

                    var ttsText = poi.GetTtsText(activeLanguage);
                    System.Diagnostics.Debug.WriteLine($"[NARRATION]   TTS text: '{ttsText}'");

                    if (!string.IsNullOrWhiteSpace(ttsText))
                    {
                        await SpeakWithTtsAsync(ttsText, locale);
                        _isSpeaking = false;
                        NarrationFinished?.Invoke(this, EventArgs.Empty);
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"[NARRATION]   TTS text is empty for '{activeLanguage}' — trying audio file or fallback");
                }

                // --- Priority 2: Audio file ---
                if (!string.IsNullOrWhiteSpace(poi.AudioFile))
                {
                    System.Diagnostics.Debug.WriteLine($"[NARRATION]   Loading audio file: '{poi.AudioFile}'");
                    try
                    {
                        var stream = await FileSystem.OpenAppPackageFileAsync(poi.AudioFile);
                        _currentPlayer = _audioManager.CreatePlayer(stream);
                        _audioCompletionSource = new TaskCompletionSource<bool>();
                        _currentPlayer.PlaybackEnded += (s, e) =>
                        {
                            _audioCompletionSource?.TrySetResult(true);
                        };
                        _currentPlayer.Play();
                        System.Diagnostics.Debug.WriteLine($"[NARRATION]   ✅ Audio playback started: '{poi.AudioFile}'");
                        await _audioCompletionSource.Task;
                        _isSpeaking = false;
                        NarrationFinished?.Invoke(this, EventArgs.Empty);
                        return;
                    }
                    catch (Exception audioEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[NARRATION]   ⚠️ Audio file failed ({audioEx.Message}) — falling back to TTS");
                    }
                }

                // --- Fallback: TTS generic text (covers UseTts=false + no audio file) ---
                var fallbackLanguage = _currentLanguage;
                var fallbackLocale = GetLocaleFromCache(_currentLanguage);

                if (fallbackLocale != null && !fallbackLocale.Language.StartsWith(_currentLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    fallbackLanguage = "en";
                }

                var fallbackText = poi.GetTtsText(fallbackLanguage);
                if (!string.IsNullOrWhiteSpace(fallbackText))
                {
                    System.Diagnostics.Debug.WriteLine($"[NARRATION]   Using fallback TTS: '{fallbackText}'");
                    await SpeakWithTtsAsync(fallbackText, fallbackLocale);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[NARRATION]   ❌ No narration content available for '{poi.Name}'");
                }

                _isSpeaking = false;
                NarrationFinished?.Invoke(this, EventArgs.Empty);
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
        private async Task SpeakWithTtsAsync(string text, Locale? locale = null)
        {
            _ttsCancelToken = new CancellationTokenSource();
            locale ??= GetLocaleFromCache(_currentLanguage);

            _lastTtsText = text;
            _lastTtsLocale = locale;
            _isTtsPaused = false;

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
                
                _audioCompletionSource?.TrySetCanceled();
                _audioCompletionSource = null;

                _isSpeaking = false;

                // Brief delay to let the TTS engine settle before a new SpeakAsync call.
                await Task.Delay(150);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NARRATION] StopNarration error: {ex.Message}");
            }
            finally
            {
                if (_playStartTime.HasValue && _currentPlayingPoiId.HasValue)
                {
                    var duration = (int)(DateTime.UtcNow - _playStartTime.Value).TotalSeconds;
                    _ = _trackingService.TrackEventAsync(_currentPlayingPoiId.Value, "audio_played", duration);
                    _playStartTime = null;
                    _currentPlayingPoiId = null;
                }
            }
        }

        public Task<bool> IsSpeakingAsync()
        {
            return Task.FromResult(_isSpeaking || _currentPlayer?.IsPlaying == true);
        }

        /// <summary>
        /// Returns the best available locale for the given language code using the
        /// pre-cached locale list. Always non-blocking. Returns fallback "en" if not found.
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

            // Fallback to English if the requested language is not supported
            if (locale == null)
            {
                System.Diagnostics.Debug.WriteLine($"[NARRATION]   No locale for '{languageCode}' — falling back to 'en-US'");
                locale = _cachedLocales.FirstOrDefault(l =>
                    l.Language.Equals("en", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(l.Country, "US", StringComparison.OrdinalIgnoreCase));

                // If en-US is not found, fallback to any 'en'
                if (locale == null)
                {
                    locale = _cachedLocales.FirstOrDefault(l =>
                        l.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase));
                }
            }

            if (locale != null)
                System.Diagnostics.Debug.WriteLine($"[NARRATION]   Locale applied: {locale.Language}-{locale.Country}");
            else
                System.Diagnostics.Debug.WriteLine($"[NARRATION]   Fallback locale not found — using system default");

            return locale;
        }
    }
}
