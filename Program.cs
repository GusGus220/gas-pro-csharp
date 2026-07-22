using System;
using System.Collections.Generic;
using System.IO;
using System.Speech.Synthesis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using NAudio.Wave;
using Vosk;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Iniciando entorno GasAI (C# Core con motor Vosk)...");

        // --- 1. CEREBRO (LLAMA 3) ---
        string modelPath = @"models\Llama-3.2-3B-Instruct-Q4_K_M.gguf";
        var parameters = new ModelParams(modelPath) { ContextSize = 2048, GpuLayerCount = 35 };
        using var model = LLamaWeights.LoadFromFile(parameters);
        using var context = model.CreateContext(parameters);
        var executor = new InteractiveExecutor(context);

        var inferenceParams = new InferenceParams()
        {
            MaxTokens = 200,
            SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline() { Temperature = 0.4f },
            AntiPrompts = new List<string> { "<|eot_id|>", "Usuario:", "User:" }
        };

        // --- 2. BOCA (TTS) ---
        using var synthesizer = new SpeechSynthesizer();
        synthesizer.SetOutputToDefaultAudioDevice();
        synthesizer.Rate = 2;

        // --- 3. OÍDOS (VOSK CONTINUO) ---
        Vosk.Vosk.SetLogLevel(0); // Silencia la consola de Vosk
        using var voskModel = new Vosk.Model(@"models\vosk-model-small-es-0.42");
        using var recognizer = new VoskRecognizer(voskModel, 16000.0f);

        // Buscador automático del micrófono (evita capturar la música del sistema)
        int micDeviceIndex = 0;
        for (int n = 0; n < WaveIn.DeviceCount; n++)
        {
            var caps = WaveIn.GetCapabilities(n);
            string nombre = caps.ProductName.ToLower();
            if ((nombre.Contains("mic") || nombre.Contains("array") || nombre.Contains("audio"))
                && !nombre.Contains("stereo") && !nombre.Contains("mezcla") && !nombre.Contains("mix"))
            {
                micDeviceIndex = n;
                break;
            }
        }

        Console.WriteLine("\n[SISTEMA LISTO] Habla con naturalidad de corrido (ej. 'Gas, ¿qué hora es?').");

        bool esPrimerMensaje = true;

        while (true)
        {
            Console.Write("\r💤 Escuchando en segundo plano...           ");
            string promptExtraido = "";

            // Abrimos el flujo del micrófono
            using var waveIn = new WaveInEvent
            {
                DeviceNumber = micDeviceIndex,
                WaveFormat = new WaveFormat(16000, 16, 1)
            };

            waveIn.DataAvailable += (s, a) =>
            {
                // Vosk evalúa el audio en tiempo real. 
                // Retorna 'true' cuando detecta que hiciste una pausa al terminar tu frase.
                if (recognizer.AcceptWaveform(a.Buffer, a.BytesRecorded))
                {
                    string jsonResult = recognizer.Result();
                    using var doc = JsonDocument.Parse(jsonResult);
                    string text = doc.RootElement.GetProperty("text").GetString()?.ToLower() ?? "";

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        // Limpiamos la frase y buscamos nuestra palabra clave
                        string textoLimpio = text.Replace(".", "").Replace(",", "").Trim();
                        int indiceGas = textoLimpio.IndexOf("gas");

                        if (indiceGas != -1)
                        {
                            // Extraemos solo lo que dijiste después de "gas" o "gas pro"
                            string prompt = textoLimpio.Substring(indiceGas + 3).Trim();
                            if (prompt.StartsWith("pro")) prompt = prompt.Substring(3).Trim();

                            if (!string.IsNullOrWhiteSpace(prompt))
                            {
                                promptExtraido = prompt; // Mandamos la señal para romper el bucle pasivo
                            }
                        }
                    }
                }
            };

            waveIn.StartRecording();

            // Mantenemos el hilo principal esperando hasta que se extraiga una pregunta válida
            while (string.IsNullOrEmpty(promptExtraido))
            {
                Thread.Sleep(50);
            }

            waveIn.StopRecording(); // Apagamos el micro mientras la IA piensa y habla

            Console.WriteLine($"\n🔔 ¡Activado! Usuario: {promptExtraido}");

            if (promptExtraido.Contains("salir") || promptExtraido.Contains("apágate")) break;

            // --- 4. CEREBRO RESPONDE (LLAMA 3) ---
            Console.Write("GAS PRO: ");
            string bufferOracion = "";
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

            await foreach (var text in executor.InferAsync(promptFinal, inferenceParams))
            {
                Console.Write(text);
                bufferOracion += text;

                if (text.Contains('.') || text.Contains('?') || text.Contains('!') || text.Contains('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(bufferOracion))
                    {
                        synthesizer.SpeakAsync(bufferOracion);
                        bufferOracion = "";
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(bufferOracion))
            {
                synthesizer.SpeakAsync(bufferOracion);
            }

            // Candado anti-eco: Esperamos a que termine de hablar antes de encender los oídos de nuevo
            while (synthesizer.State == SynthesizerState.Speaking)
            {
                Thread.Sleep(200);
            }
            Thread.Sleep(500);

            Console.WriteLine("\n");
        }
    }
}