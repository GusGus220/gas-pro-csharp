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
        private readonly WindowsControlService _windowsService;

        public GasOrchestrator()
        {
            _llamaService = new LlamaService();
            _speechService = new SpeechService();
            _audioService = new AudioService();
            _windowsService = new WindowsControlService();
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

                // ---- 🚨 RUTA DE REFLEJOS (FAST-PATH MEJORADA) ----
                bool esComandoDeSistema = true;
                string comando = promptExtraido.ToLower();

                // 1. ABRIR APLICACIONES (Usando protocolos de Windows)
                if (comando.Contains("spotify"))
                {
                    _speechService.SpeakAsync("Abriendo Spotify.");
                    _windowsService.OpenApplication("spotify:"); // Los dos puntos son la clave mágica
                }
                else if (comando.Contains("chrome") || comando.Contains("google") || comando.Contains("navegador"))
                {
                    _speechService.SpeakAsync("Abriendo el navegador.");
                    _windowsService.OpenApplication("https://www.google.com"); // Esto fuerza abrir tu navegador default
                }
                // 2. MULTIMEDIA
                else if (comando.Contains("pausa") || comando.Contains("reanuda") || comando.Contains("reproduce"))
                {
                    _speechService.SpeakAsync("Hecho.");
                    _windowsService.PlayPauseMusic();
                }
                // 3. CONTROL DE VOLUMEN INTELIGENTE
                else if (comando.Contains("volumen"))
                {
                    esComandoDeSistema = true;
                    int targetVolume = -1;
                    string[] palabras = comando.Split(' ');

                    // Buscamos si dijiste un porcentaje exacto
                    foreach (var palabra in palabras)
                    {
                        string numStr = palabra.Replace("%", "").Trim();
                        if (int.TryParse(numStr, out int num)) { targetVolume = num; break; }

                        // Diccionario casero por si Vosk lo escribe con letras
                        if (numStr == "cero") targetVolume = 0;
                        else if (numStr == "diez") targetVolume = 10;
                        else if (numStr == "veinte") targetVolume = 20;
                        else if (numStr == "treinta") targetVolume = 30;
                        else if (numStr == "cuarenta") targetVolume = 40;
                        else if (numStr == "cincuenta") targetVolume = 50;
                        else if (numStr == "sesenta") targetVolume = 60;
                        else if (numStr == "setenta") targetVolume = 70;
                        else if (numStr == "ochenta") targetVolume = 80;
                        else if (numStr == "noventa") targetVolume = 90;
                        else if (numStr == "cien" || numStr == "ciento") targetVolume = 100;
                    }

                    if (targetVolume != -1)
                    {
                        _speechService.SpeakAsync($"Ajustando el volumen al {targetVolume} por ciento.");
                        _windowsService.SetVolume(targetVolume);
                    }
                    else if (comando.Contains("baj") || comando.Contains("disminu"))
                    {
                        _speechService.SpeakAsync("Bajando el volumen.");
                        _windowsService.ChangeVolumeBy(-20); // Baja 20% de golpe
                    }
                    else if (comando.Contains("sub") || comando.Contains("aument"))
                    {
                        _speechService.SpeakAsync("Subiendo el volumen.");
                        _windowsService.ChangeVolumeBy(20); // Sube 20% de golpe
                    }
                }
                // SI NO ES NINGUNO DE ARRIBA, PASA A LLAMA 3
                else
                {
                    esComandoDeSistema = false;
                }

                if (esComandoDeSistema)
                {
                    _speechService.WaitForSpeechToFinish();
                    continue;
                }
                // ------------------------------------------------

                // 2. Preparar el Prompt para Llama 3
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

                // 4. Candado anti-eco
                _speechService.WaitForSpeechToFinish();
                Console.WriteLine("\n");
            }
        }
    }
}