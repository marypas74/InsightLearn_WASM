using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Application.DTOs;

/// <summary>
/// Request per inviare un messaggio al chatbot
/// </summary>
public class ChatMessageRequest
{
    /// <summary>
    /// Il messaggio dell'utente
    /// </summary>
    [Required(ErrorMessage = "Il messaggio è obbligatorio")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Il messaggio deve essere tra 1 e 2000 caratteri")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Email dell'utente (opzionale, per tracking)
    /// </summary>
    [EmailAddress(ErrorMessage = "Email non valida")]
    public string? Email { get; set; }

    /// <summary>
    /// ID della sessione per continuità conversazione
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// ID del corso a cui è relativo il messaggio (opzionale)
    /// </summary>
    public Guid? CourseId { get; set; }
}

/// <summary>
/// Response dal chatbot
/// </summary>
public class ChatMessageResponse
{
    /// <summary>
    /// La risposta generata dal chatbot
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// ID della sessione per continuità conversazione
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp della risposta
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tempo impiegato per generare la risposta (ms)
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// Modello AI usato
    /// </summary>
    public string? AiModel { get; set; }

    /// <summary>
    /// Indica se c'è stato un errore
    /// </summary>
    public bool HasError { get; set; }

    /// <summary>
    /// Messaggio di errore (se presente)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO per ottenere lo storico chat
/// </summary>
public class ChatHistoryDto
{
    /// <summary>
    /// ID del messaggio
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID della sessione
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Il messaggio
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// True se è un messaggio dell'utente, False se è del bot
    /// </summary>
    public bool IsUserMessage { get; set; }

    /// <summary>
    /// Timestamp del messaggio
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Modello AI usato (solo per messaggi del bot)
    /// </summary>
    public string? AiModel { get; set; }
}

/// <summary>
/// Request per ottenere lo storico chat
/// </summary>
public class ChatHistoryRequest
{
    /// <summary>
    /// ID della sessione
    /// </summary>
    [Required(ErrorMessage = "SessionId è obbligatorio")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Numero massimo di messaggi da restituire
    /// </summary>
    public int? Limit { get; set; } = 50;
}

/// <summary>
/// Request per pulire lo storico chat (quando cookie consent viene cancellato)
/// </summary>
public class ClearChatHistoryRequest
{
    /// <summary>
    /// ID della sessione da pulire
    /// </summary>
    [Required(ErrorMessage = "SessionId è obbligatorio")]
    public string SessionId { get; set; } = string.Empty;
}
