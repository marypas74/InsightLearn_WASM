namespace InsightLearn.Application.Interfaces;

/// <summary>
/// Servizio per comunicare con Ollama LLM API
/// </summary>
public interface IOllamaService
{
    /// <summary>
    /// Genera una risposta dal modello LLM
    /// </summary>
    /// <param name="prompt">Il messaggio dell'utente</param>
    /// <param name="model">Il nome del modello da usare (default: configurato in appsettings)</param>
    /// <param name="cancellationToken">Token per cancellazione</param>
    /// <returns>La risposta generata dal modello</returns>
    Task<string> GenerateResponseAsync(string prompt, string? model = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se il servizio Ollama è disponibile
    /// </summary>
    /// <returns>True se il servizio risponde, False altrimenti</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Ottiene la lista dei modelli disponibili
    /// </summary>
    /// <returns>Lista dei nomi dei modelli</returns>
    Task<List<string>> GetAvailableModelsAsync();
}
