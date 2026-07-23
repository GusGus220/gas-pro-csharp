using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;
using PiperSharp;
using PiperSharp.Models;

namespace GasPro.Services
{
    public class PiperSpeechService
    {
        private PiperProvider _piperProvider;
        private bool _isInitialized = false;

        public async Task InitializeAsync(string modelKey, string modelsDirectory)
        {
            try
            {
                Console.WriteLine("[PiperSharp] Conectando con el motor de NuGet...");

                string cwd = Directory.GetCurrentDirectory();
                string piperDir = Path.Combine(cwd, "piper");

                // PiperSharp usa "Keys" (ej. "es_ES-mls-medium") en lugar de extensiones
                modelKey = modelKey.Replace(".onnx", "");

                // 1. Descarga automática del motor Piper (solo pasa la primera vez)
                if (!Directory.Exists(piperDir) || !File.Exists(Path.Combine(piperDir, "piper.exe")))
                {
                    Console.WriteLine("[PiperSharp] Descargando motor base de Piper (Esto tomará unos segundos)...");
                    await PiperDownloader.DownloadPiper().ExtractPiper(cwd);
                }

                // 2. Busca la voz en los servidores y la descarga automáticamente
                Console.WriteLine($"[PiperSharp] Descargando/Verificando modelo '{modelKey}'...");
                var modelInfo = await PiperDownloader.GetModelByKey(modelKey);
                var downloadedModel = await modelInfo.DownloadModel(cwd);

                // 3. Inicializa el proveedor de voz real
                _piperProvider = new PiperProvider(new PiperConfiguration()
                {
                    ExecutableLocation = Path.Combine(piperDir, "piper.exe"),
                    WorkingDirectory = piperDir,
                    Model = downloadedModel
                });

                _isInitialized = true;
                Console.WriteLine("[PiperSharp] ¡Modelo y motor listos para hablar!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error crítico al iniciar PiperSharp] {ex.Message}");
            }
        }

        public void SpeakAsync(string text)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(text)) return;

            Task.Run(async () =>
            {
                try
                {
                    // PiperSharp convierte el texto a audio y devuelve un array de bytes (formato WAV)
                    byte[] audioBytes = await _piperProvider.InferAsync(text, AudioOutputType.Wav);

                    // Reproducimos al vuelo desde la memoria usando NAudio (cero delay de disco)
                    using (var ms = new MemoryStream(audioBytes))
                    using (var waveReader = new WaveFileReader(ms))
                    using (var outputDevice = new WaveOutEvent())
                    {
                        outputDevice.Init(waveReader);
                        outputDevice.Play();
                        while (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            await Task.Delay(10);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[Error de Síntesis Piper] {ex.Message}");
                }
            });
        }

        public void WaitForSpeechToFinish()
        {
            Task.Delay(200).Wait();
        }
    }
}