using Plugin.Maui.Audio;

namespace FoodStreetApp.Services
{
    public class AudioService : IAudioService
    {
        private readonly IAudioManager _audioManager;
        private IAudioPlayer? _currentPlayer;

        public AudioService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        public async Task PlayAudioAsync(string audioFile)
        {
            try
            {
                await StopAudioAsync();

                var stream = await FileSystem.OpenAppPackageFileAsync(audioFile);
                _currentPlayer = _audioManager.CreatePlayer(stream);
                _currentPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing audio: {ex.Message}");
            }
        }

        public async Task StopAudioAsync()
        {
            if (_currentPlayer != null)
            {
                _currentPlayer.Stop();
                _currentPlayer.Dispose();
                _currentPlayer = null;
            }
            await Task.CompletedTask;
        }
    }
}
