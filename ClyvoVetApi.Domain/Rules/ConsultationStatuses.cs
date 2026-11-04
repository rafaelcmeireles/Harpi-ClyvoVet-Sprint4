namespace ClyvoVetApi.Domain
{
    /// <summary>
    /// Status possíveis de uma consulta veterinária e as transições permitidas entre eles.
    /// </summary>
    public static class ConsultationStatuses
    {
        public const string Agendada = "Agendada";
        public const string Realizada = "Realizada";
        public const string Cancelada = "Cancelada";

        public static readonly IReadOnlyCollection<string> All = new[] { Agendada, Realizada, Cancelada };

        public static bool IsValid(string? status) =>
            !string.IsNullOrWhiteSpace(status) && All.Contains(status, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Uma consulta em estado final (Realizada ou Cancelada) não pode mais ser alterada.
        /// A partir de "Agendada", qualquer status válido pode ser assumido.
        /// </summary>
        public static bool CanTransition(string currentStatus, string newStatus)
        {
            if (!IsValid(currentStatus) || !IsValid(newStatus))
                return false;

            var isCurrentFinal =
                string.Equals(currentStatus, Realizada, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentStatus, Cancelada, StringComparison.OrdinalIgnoreCase);

            return !isCurrentFinal;
        }
    }
}
