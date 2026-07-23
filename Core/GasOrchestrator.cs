using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GasPro.Services;

namespace GasPro.Core
{
    // Estructura interna de mensaje para la memoria de corto plazo
    public class LocalChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    public class GasOrchestrator
    {
        private readonly LlamaService _llamaService;
        private readonly PiperSpeechService _speechService;
        private readonly AudioService _audioService;
        private readonly WindowsControlService _windowsService;

        private readonly List<LocalChatMessage> _chatHistory;
        private const int MaxHistorySize = 2;

        public GasOrchestrator()
        {
            _llamaService = new LlamaService();
            _speechService = new PiperSpeechService();
            _audioService = new AudioService();
            _windowsService = new WindowsControlService();
            _chatHistory = new List<LocalChatMessage>();
        }

        public void Initialize(string llamaModelPath, string voskModelPath)
        {
            Console.WriteLine("Iniciando entorno GAS PRO (C# Core unificado)...");

            _llamaService.Initialize(llamaModelPath);
            _speechService.InitializeAsync("es_ES-mls-medium.onnx", "models/piper").GetAwaiter().GetResult();
            _audioService.Initialize(voskModelPath);

            Console.WriteLine("\n[SISTEMA LISTO] Habla con naturalidad de corrido (ej. 'Gas, ¿qué hora es?').");
        }

        public async Task RunAsync()
        {
            while (true)
            {
                Console.Write("\r💤 Escuchando en segundo plano...           ");

                string promptExtraido = await _audioService.ListenForPromptAsync();

                Console.WriteLine($"\n🔔 ¡Activado! Usuario: {promptExtraido}");

                if (promptExtraido.Contains("salir") || promptExtraido.Contains("apágate")) break;

                // ---- 🚨 RUTA DE REFLEJOS (FAST-PATH) ----
                bool esComandoDeSistema = true;
                string comando = promptExtraido.ToLower();

                if (comando.Contains("spotify"))
                {
                    _speechService.SpeakAsync("Abriendo Spotify.");
                    _windowsService.OpenApplication("spotify:");
                }
                else if (comando.Contains("chrome") || comando.Contains("google") || comando.Contains("navegador"))
                {
                    _speechService.SpeakAsync("Abriendo el navegador.");
                    _windowsService.OpenApplication("https://www.google.com");
                }
                else if (comando.Contains("abre discord"))
                {
                    _speechService.SpeakAsync("Abriendo Discord.");
                    _ = Task.Run(() => _windowsService.OpenAppBySearch("discord"));
                }
                else if (comando.Contains("hora") || comando.Contains("qué hora es"))
                {
                    string horaFormateada = DateTime.Now.ToString("h:mm tt", new System.Globalization.CultureInfo("es-ES"))
                        .Replace("AM", "de la mañana").Replace("PM", "de la tarde");
                    string mensajeHora = $"Son las {horaFormateada}";
                    Console.WriteLine($"GAS PRO: {mensajeHora}");
                    _speechService.SpeakAsync(mensajeHora);
                }
                else if (comando.Contains("fecha") || comando.Contains("día es hoy") || comando.Contains("dia es hoy"))
                {
                    string fechaFormateada = DateTime.Now.ToString("dddd, d 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));
                    string mensajeFecha = $"Hoy es {fechaFormateada}";
                    Console.WriteLine($"GAS PRO: {mensajeFecha}");
                    _speechService.SpeakAsync(mensajeFecha);
                }
                else if (comando.Contains("haz clic") || comando.Contains("haz click"))
                {
                    _speechService.SpeakAsync("Clic hecho.");
                    _windowsService.LeftClick();
                }
                else if (comando.Contains("pausa") || comando.Contains("reanuda") || comando.Contains("reproduce"))
                {
                    _speechService.SpeakAsync("Hecho.");
                    _windowsService.PlayPauseMusic();
                }
                else if (comando.Contains("volumen"))
                {
                    int targetVolume = -1;
                    string[] palabras = comando.Split(' ');

                    foreach (var palabra in palabras)
                    {
                        string numStr = palabra.Replace("%", "").Trim();
                        if (int.TryParse(numStr, out int num)) { targetVolume = num; break; }

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
                        _windowsService.ChangeVolumeBy(-20);
                    }
                    else if (comando.Contains("sub") || comando.Contains("aument"))
                    {
                        _speechService.SpeakAsync("Subiendo el volumen.");
                        _windowsService.ChangeVolumeBy(20);
                    }
                }
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

                // --- CONSTRUCCIÓN DEL PROMPT CON MEMORIA ---
                Console.Write("GAS PRO: ");

                string systemPrompt = "<|start_header_id|>system<|end_header_id|>\n\nEres GAS PRO, asistente de IA avanzado. Ubicación: Piura, Perú. Respuestas concisas, precisas y basadas en hechos.<|eot_id|>";
                string promptFinal = systemPrompt;

                foreach (var msg in _chatHistory)
                {
                    if (msg.Role == "user")
                        promptFinal += $"<|start_header_id|>user<|end_header_id|>\n\n{msg.Content}<|eot_id|>";
                    else
                        promptFinal += $"<|start_header_id|>assistant<|end_header_id|>\n\n{msg.Content}<|eot_id|>";
                }

                promptFinal += $"<|start_header_id|>user<|end_header_id|>\n\n{promptExtraido}<|eot_id|><|start_header_id|>assistant<|end_header_id|>\n\n";

                string respuestaCompleta = "";
                string bufferOracion = "";

                await foreach (var text in _llamaService.GenerateResponseStreamAsync(promptFinal))
                {
                    Console.Write(text);
                    bufferOracion += text;
                    respuestaCompleta += text;

                    if (text.Contains(' ') || text.Contains('.') || text.Contains(',') || text.Contains('?') || text.Contains('!'))
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

                _chatHistory.Add(new LocalChatMessage { Role = "user", Content = promptExtraido });
                _chatHistory.Add(new LocalChatMessage { Role = "assistant", Content = respuestaCompleta.Trim() });

                while (_chatHistory.Count > MaxHistorySize)
                {
                    _chatHistory.RemoveAt(0);
                }

                _speechService.WaitForSpeechToFinish();
                Console.WriteLine("\n");
            }
        }
    }
}