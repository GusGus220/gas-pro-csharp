using System;
using LLama;
using LLama.Common;
using System.Speech.Synthesis;
using NAudio.Wave;
using Whisper.net;

Console.WriteLine("Iniciando GAS PRO Core...");

string modelPath = @"models\Llama-3.2-3B-Instruct-Q4_K_M.gguf";

var parameters = new ModelParams(modelPath)
{
    ContextSize = 2048,
    GpuLayerCount = 35 // ¡Magia para tu RTX!
};

// 1. Cargamos el modelo
using var model = LLamaWeights.LoadFromFile(parameters);
using var context = model.CreateContext(parameters);

var executor = new InteractiveExecutor(context);

// Motor de voz nativo (La Boca)
using var synthesizer = new System.Speech.Synthesis.SpeechSynthesizer();
synthesizer.SetOutputToDefaultAudioDevice();
synthesizer.Rate = 2;

// Frenos de mano y termostato ajustado
var inferenceParams = new InferenceParams()
{
    MaxTokens = 200,
    SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline() { Temperature = 0.4f },
    AntiPrompts = new List<string> { "<|eot_id|>", "Usuario:", "User:" }
};

Console.WriteLine("\n¡Cerebro Llama 3 Nativo Conectado! Escribe un mensaje:");

// La bandera que controla la inyección del alma
bool esPrimerMensaje = true;

while (true)
{
    Console.Write("\nUsuario: ");
    string prompt = Console.ReadLine() ?? "";

    if (prompt.ToLower() == "salir") break;

    Console.Write("GAS PRO: ");
    string bufferOracion = "";
    string promptFinal = "";

    if (esPrimerMensaje)
    {
        // ELIMINAMOS el <|begin_of_text|> que causaba el cortocircuito.
        // Además, le damos una instrucción estricta de usar hechos científicos para que no invente síndromes.
        string systemPrompt = "<|start_header_id|>system<|end_header_id|>\n\nEres GAS PRO, asistente de IA. Ubicación: Piura, Perú. Responde con hechos científicos, reales y precisos. Cero alucinaciones. No inventes historias.<|eot_id|>";

        promptFinal = systemPrompt + $"<|start_header_id|>user<|end_header_id|>\n\n{prompt}<|eot_id|><|start_header_id|>assistant<|end_header_id|>\n\n";

        esPrimerMensaje = false;
    }
    else
    {
        // 2. MODO CONTINUO: Para los siguientes mensajes, solo inyectamos tu texto en su idioma nativo
        promptFinal = $"<|start_header_id|>user<|end_header_id|>\n\n{prompt}<|eot_id|><|start_header_id|>assistant<|end_header_id|>\n\n";
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
}