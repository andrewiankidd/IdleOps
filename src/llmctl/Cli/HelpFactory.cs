namespace llmctl.Cli;

internal static class HelpFactory
{
    public static void PrintHelp()
    {
        IdleOps.Shared.Cli.HelpPrinter.PrintRaw("llmctl", """
            Usage: llmctl --goal "<prompt>" [--image <screenshot.png>] [--system "<instructions>"]

            Thin client for an OpenAI-compatible chat endpoint (Ollama, vLLM, LM Studio).
            The model runs externally on the GPU; llmctl just asks it and prints the reply.
            Give an --image to reason over a screenshot with a vision model.

            Prompt:
              -g, --goal <text>      The task/question (required)
              -i, --image <path>     Screenshot to include (PNG; needs a vision model)
              -s, --system <text>    System prompt, e.g. constrain output to JSON
              --temperature <n>      Sampling temperature (default 0.2)

            Backend (defaults suit a local Ollama install):
              --endpoint <url>       OpenAI-compatible base URL (env IDLEOPS_LLM_ENDPOINT,
                                     default http://localhost:11434/v1)
              --model <name>         Model name (env IDLEOPS_LLM_MODEL, default qwen2.5vl:7b)
              --api-key <key>        Bearer token if the endpoint needs one (env IDLEOPS_LLM_API_KEY)

            Other:
              -h, --help             Show help

            Examples:
              llmctl --goal "What is on this screen? Reply in one line." --image shot.png
              llmctl --goal "A click on Save failed. What is blocking it and which key dismisses it?" \
                     --image shot.png --system "Reply as JSON: {\"diagnosis\":\"\",\"key\":\"\"}"
            """);
    }
}
