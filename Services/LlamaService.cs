using LLama;
using LLama.Common;
using System.Collections.Generic;

namespace GasPro.Services
{
    public class LlamaService
    {
        private LLamaWeights _model;
        private LLamaContext _context;
        private InteractiveExecutor _executor;
        private InferenceParams _inferenceParams;

        public void Initialize(string modelPath)
        {
            var parameters = new ModelParams(modelPath) { ContextSize = 2048, GpuLayerCount = 35 };
            _model = LLamaWeights.LoadFromFile(parameters);
            _context = _model.CreateContext(parameters);
            _executor = new InteractiveExecutor(_context);

            _inferenceParams = new InferenceParams()
            {
                MaxTokens = 200,
                SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline() { Temperature = 0.4f },
                AntiPrompts = new List<string> { "<|eot_id|>", "Usuario:", "User:" }
            };
        }

        // Devolvemos los tokens uno a uno para tu streaming en vivo
        public async IAsyncEnumerable<string> GenerateResponseStreamAsync(string promptFinal)
        {
            await foreach (var text in _executor.InferAsync(promptFinal, _inferenceParams))
            {
                yield return text;
            }
        }
    }
}