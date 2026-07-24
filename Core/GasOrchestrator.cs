using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GasPro.Services;
using Spectre.Console;

namespace GasPro.Core
{
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
            // 1. Silenciamos Vosk
            Vosk.Vosk.SetLogLevel(-1);

            Console.WriteLine("Iniciando motores (ignora los textos raros de C++ que saldrán ahora)...");

            // 2. Inicializamos los motores UNA SOLA VEZ con el modelo correcto
            _llamaService.Initialize(llamaModelPath);
            _speechService.InitializeAsync("es_ES-sharvard-medium", "models/piper").GetAwaiter().GetResult();
            _audioService.Initialize(voskModelPath);

            // 3. ✨ EL PRE-CALENTAMIENTO (LA TRAMPA) ✨
            // Obligamos a Llama a pensar para que escupa los logs AHORA
            try
            {
                var enumerador = _llamaService.GenerateResponseStreamAsync("a").GetAsyncEnumerator();
                enumerador.MoveNextAsync().AsTask().GetAwaiter().GetResult();
            }
            catch { }

            // 4. ✨ EL PLUMAZO ✨ Borramos la basura de C++
            Console.Clear();

            // 🎨 5. Título Hacker en Arte ASCII
            AnsiConsole.Write(
                new FigletText("GAS PRO")
                    .Centered()
                    .Color(Color.SpringGreen3));

            // 🎨 6. Panel elegante de estado de motores
            var panelDeCarga = new Panel(
                "Arquitectura modular C# inicializada y pre-calentada.\n\n" +
                "🧠 [bold blue]Cerebro:[/] Llama 3 (Grafo neuronal activo)\n" +
                "🗣️ [bold green]Voz:[/] Piper Neural (Medium predictivo)\n" +
                "👂 [bold yellow]Escucha:[/] Vosk Offline API")
                .Header("[bold white] Secuencia de Arranque [/]")
                .BorderColor(Color.Cyan1)
                .Padding(2, 1, 2, 1);

            AnsiConsole.Write(panelDeCarga);

            AnsiConsole.MarkupLine("\n[bold springgreen3]SISTEMA LISTO Y OPERATIVO[/] 🚀");
        }

        public async Task RunAsync()
        {
            while (true)
            {
                // 🎨 Separador visual para cada nueva iteración
                AnsiConsole.Write(new Rule("[dim]Esperando comando de voz...[/]").RuleStyle("grey").LeftJustified());

                string promptExtraido = await _audioService.ListenForPromptAsync();

                // 🎨 Tu mensaje en amarillo
                AnsiConsole.MarkupLine($"\n👤 [bold yellow]Tú:[/] {promptExtraido}");

                if (promptExtraido.Contains("salir") || promptExtraido.Contains("apágate")) break;

                // ---- 🚨 RUTA DE REFLEJOS ----
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
                    AnsiConsole.MarkupLine($"🤖 [bold cyan]GAS PRO:[/] {mensajeHora}");
                    _speechService.SpeakAsync(mensajeHora);
                }
                else if (comando.Contains("fecha") || comando.Contains("día es hoy") || comando.Contains("dia es hoy"))
                {
                    string fechaFormateada = DateTime.Now.ToString("dddd, d 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));
                    string mensajeFecha = $"Hoy es {fechaFormateada}";
                    AnsiConsole.MarkupLine($"🤖 [bold cyan]GAS PRO:[/] {mensajeFecha}");
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
                    Console.WriteLine("\n");
                    continue;
                }
                // ------------------------------------------------

                // --- CONSTRUCCIÓN DEL PROMPT CON MEMORIA ---
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

                // 🎨 Etiqueta de la IA
                AnsiConsole.Markup("🤖 [bold cyan]GAS PRO:[/] ");
                Console.ForegroundColor = ConsoleColor.Cyan;

                // 3. Pensar y Hablar en Streaming Inteligente (Puntuación)
                await foreach (var text in _llamaService.GenerateResponseStreamAsync(promptFinal))
                {
                    Console.Write(text);
                    bufferOracion += text;
                    respuestaCompleta += text;

                    if (text.Contains('.') || text.Contains(',') || text.Contains('?') || text.Contains('!') || text.Contains('\n'))
                    {
                        if (!string.IsNullOrWhiteSpace(bufferOracion))
                        {
                            _speechService.SpeakAsync(bufferOracion);
                            bufferOracion = "";
                        }
                    }
                }

                Console.ResetColor();

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