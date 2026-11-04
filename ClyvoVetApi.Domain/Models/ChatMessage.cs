namespace ClyvoVetApi.Models
{
    /// <summary>
    /// Mensagem do chat interno entre tutor e veterinário, vinculada a uma consulta.
    /// Persistida em MongoDB (NoSQL), pois é um dado append-only, de alto volume e sem joins.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>ObjectId (string) gerado pelo MongoDB.</summary>
        public string Id { get; set; } = string.Empty;

        public int ConsultationId { get; set; }

        public int SenderId { get; set; }

        public string SenderRole { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }
    }
}
