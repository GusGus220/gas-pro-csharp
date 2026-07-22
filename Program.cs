using System.Threading.Tasks;
using GasPro.Core;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Instanciamos el cerebro central
        var orchestrator = new GasOrchestrator();

        // 2. Definimos las rutas de los modelos
        string llamaModelPath = @"models\Llama-3.2-3B-Instruct-Q4_K_M.gguf";
        string voskModelPath = @"models\vosk-model-small-es-0.42";

        // 3. Inicializamos e iniciamos el bucle
        orchestrator.Initialize(llamaModelPath, voskModelPath);
        await orchestrator.RunAsync();
    }
}