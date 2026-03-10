using Plugin.Maui.Audio;

namespace FoodStreetApp.Services
{
    public interface IAudioService
    {
        Task PlayAudioAsync(string audioFile);
        Task StopAudioAsync();
    }
}
