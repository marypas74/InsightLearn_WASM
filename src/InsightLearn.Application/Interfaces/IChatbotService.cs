using InsightLearn.Application.DTOs;

namespace InsightLearn.Application.Interfaces;

/// <summary>
/// Servizio per gestire le conversazioni del chatbot
/// </summary>
public interface IChatbotService
{
    /// <summary>
    /// Processa un messaggio dell'utente e genera una risposta dal chatbot
    /// Salva sia il messaggio dell'utente che la risposta del bot su database
    /// </summary>
    /// <param name="request">La richiesta con il messaggio dell'utente</param>
    /// <param name="ipAddress">Indirizzo IP dell'utente (per tracking)</param>
    /// <param name="userAgent">User agent dell'utente (per tracking)</param>
    /// <param name="cancellationToken">Token per cancellazione</param>
    /// <returns>La risposta del chatbot</returns>
    Task<ChatMessageResponse> ProcessMessageAsync(
        ChatMessageRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene lo storico dei messaggi per una sessione specifica
    /// </summary>
    /// <param name="sessionId">ID della sessione</param>
    /// <param name="limit">Numero massimo di messaggi da restituire</param>
    /// <returns>Lista di messaggi della conversazione</returns>
    Task<List<ChatHistoryDto>> GetChatHistoryAsync(string sessionId, int limit = 50);

    /// <summary>
    /// Pulisce lo storico della chat per una sessione specifica
    /// Chiamato quando l'utente cancella il cookie consent
    /// </summary>
    /// <param name="sessionId">ID della sessione da pulire</param>
    /// <returns>True se è stato pulito con successo</returns>
    Task<bool> ClearChatHistoryAsync(string sessionId);

    /// <summary>
    /// Ottiene o crea un contatto chatbot per una sessione
    /// </summary>
    /// <param name="sessionId">ID della sessione</param>
    /// <param name="email">Email dell'utente (opzionale)</param>
    /// <param name="initialMessage">Primo messaggio della conversazione (opzionale)</param>
    /// <param name="ipAddress">IP dell'utente (opzionale)</param>
    /// <param name="userAgent">User agent (opzionale)</param>
    /// <returns>Il contatto chatbot</returns>
    Task<Core.Entities.ChatbotContact> GetOrCreateContactAsync(
        string sessionId,
        string? email = null,
        string? initialMessage = null,
        string? ipAddress = null,
        string? userAgent = null);
}
