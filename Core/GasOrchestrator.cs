using System;
using System.Threading.Tasks;
using GasPro.Services;

namespace GasPro.Core
{
    public class GasOrchestrator
    {
        private readonly LlamaService _llamaService;
        private readonly SpeechService _speechService;
        private readonly AudioService _audioService;

        public GasOrchestrator()
        {
            _llamaService = new LlamaService();
            _speechService = new SpeechService();
            _audioService = new AudioService();
        }

        public void Initialize(string llamaModelPath, string voskModelPath)
        {
            Console.WriteLine("Iniciando entorno GasAI (C# Core con motor Vosk)...");

            _llamaService.Initialize(llamaModelPath);
            _speechService.Initialize();
            _audioService.Initialize(voskModelPath);

            Console.WriteLine("\n[SISTEMA LISTO] Habla con naturalidad de corrido (ej. 'Gas, ¿qué hora es?').");
        }

        public async Task RunAsync()
        {
            bool esPrimerMensaje = true;

            while (true)
            {
                Console.Write("\r💤 Escuchando en segundo plano...           ");

                // 1. Escuchar
                string promptExtraido = await _audioService.ListenForPromptAsync();

                Console.WriteLine($"\n🔔 ¡Activado! Usuario: {promptExtraido}");

                if (promptExtraido.Contains("salir") || promptExtraido.Contains("apágate")) break;

                // 2. Preparar el Prompt
                Console.Write("GAS PRO: ");
                string promptFinal = "";

                if (esPrimerMensaje)
                {
                    string systemPrompt = "<|start_header_id|>system<|end_header_id|>\n\nEres GAS PRO, asistente de IA. Ubicación: Piura, Perú. Responde con hechos comprobables. Cero alucinaciones. Respuestas concisas.<|eot_id|>";
                    promptFinal = systemPrompt + $"<|start_header_id|>user<|end_header_id|>\n\n{promptExtraido}<|eot_id|><|start_header_id|>assistant<|end_header_id|>\n\n";
                    esPrimerMensaje = false;
                }
                else
                {
                    promptFinal = $"<|start_header_id|>user<|end_header_id|>\n\n{promptExtraido}<|eot_id|><|start_header_id|>assistant<|end_header_id|>\n\n";
                }

                // 3. Pensar y Hablar en Streaming
                string bufferOracion = "";

                await foreach (var text in _llamaService.GenerateResponseStreamAsync(promptFinal))
                {
                    Console.Write(text);
                    bufferOracion += text;

                    // Tu lógica de pausas
                    if (text.Contains('.') || text.Contains('?') || text.Contains('!') || text.Contains('\n'))
                    {
                        if (!string.IsNullOrWhiteSpace(bufferOracion))
                        {
                            _speechService.SpeakAsync(bufferOracion);
                            bufferOracion = "";
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(bufferOracion))
                {
                    _speechService.SpeakAsync(bufferOracion);
                }

                // 4. Bloqueo para no escucharse a sí mismo
                _speechService.WaitForSpeechToFinish();
                Console.WriteLine("\n");
            }
        }
    }
}