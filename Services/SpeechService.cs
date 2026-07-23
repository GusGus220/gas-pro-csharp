using System.Speech.Synthesis;
using System.Threading;

namespace GasPro.Services
{
    public class SpeechService
    {
        private SpeechSynthesizer _synthesizer;

        public void Initialize()
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SetOutputToDefaultAudioDevice();
            _synthesizer.Rate = 3; 
        }

        public void SpeakAsync(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                _synthesizer.SpeakAsync(text);
            }
        }

        public void WaitForSpeechToFinish()
        {
            // Tu candado anti-eco original
            while (_synthesizer.State == SynthesizerState.Speaking)
            {
                Thread.Sleep(200);
            }
            Thread.Sleep(500);
        }
    }
}