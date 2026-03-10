using FoodStreetApp.Models;
using Plugin.Maui.Audio;

namespace FoodStreetApp.Services
{
    public class NarrationService : INarrationService
    {
        private readonly IAudioManager _audioManager;
        private IAudioPlayer? _currentPlayer;
        private bool _isSpeaking = false;

        public NarrationService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        public async Task PlayNarrationAsync(POI poi)
        {
            try
            {
                await StopNarrationAsync();

                _isSpeaking = true;

                if (poi.UseTts && !string.IsNullOrWhiteSpace(poi.TtsText))
                {
                    System.Diagnostics.Debug.WriteLine($">>> Playing TTS: {poi.Name}");
                    await TextToSpeech.Default.SpeakAsync(poi.TtsText);
                }
                else if (!string.IsNullOrWhiteSpace(poi.AudioFile))
                {
                    System.Diagnostics.Debug.WriteLine($">>> Playing Audio: {poi.AudioFile}");
                    var stream = await FileSystem.OpenAppPackageFileAsync(poi.AudioFile);
                    _currentPlayer = _audioManager.CreatePlayer(stream);
                    _currentPlayer.Play();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($">>> No narration configured for: {poi.Name}");
                }

                _isSpeaking = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing narration: {ex.Message}");
                _isSpeaking = false;
            }
        }

        public async Task StopNarrationAsync()
        {
            try
            {
                if (TextToSpeech.Default.GetLocalesAsync() != null)
                {
                    await TextToSpeech.Default.SpeakAsync(string.Empty);
                }

                if (_currentPlayer != null)
                {
                    _currentPlayer.Stop();
                    _currentPlayer.Dispose();
                    _currentPlayer = null;
                }

                _isSpeaking = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping narration: {ex.Message}");
            }
        }

        public Task<bool> IsSpeakingAsync()
        {
            return Task.FromResult(_isSpeaking || _currentPlayer?.IsPlaying == true);
        }
    }
}
