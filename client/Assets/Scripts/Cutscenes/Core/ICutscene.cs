using System;

namespace GameClient.Cutscenes.Core
{
    public interface ICutscene
    {
        string CutsceneId { get; }
        bool IsPlaying { get; }
        
        void Play();
        void Pause();
        void Resume();
        void Stop();
        void Skip();
    }
}
