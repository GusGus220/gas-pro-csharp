using System;
using LLama;
using LLama.Common;

Console.WriteLine("Iniciando GAS PRO Core...");

// IMPORTANTE: Asegúrate de que el nombre del archivo coincida exactamente con el tuyo
string modelPath = @"models\Llama-3.2-3B-Instruct-Q4_K_M.gguf";

var parameters = new ModelParams(modelPath)
{
    ContextSize = 2048,
    GpuLayerCount = 35 // ¡Magia para tu RTX!
};

using var model = LLamaWeights.LoadFromFile(parameters);
using var context = model.CreateContext(parameters);
var executor = new InstructExecutor(context);

Console.WriteLine("\n¡Cerebro conectado! Escribe un mensaje (o 'salir' para terminar):");

// Parámetros actualizados a la nueva versión
var inferenceParams = new InferenceParams()
{
    MaxTokens = 150
};

while (true)
{
    Console.Write("\nUsuario: ");
    string prompt = Console.ReadLine() ?? "";

    if (prompt.ToLower() == "salir") break;

    Console.Write("GAS PRO: ");

    // El motor ahora exige que la respuesta sea asíncrona (await foreach) y use InferAsync
    await foreach (var text in executor.InferAsync(prompt, inferenceParams))
    {
        Console.Write(text);
    }
}