namespace ClyvoVetApi.Domain
{
    /// <summary>
    /// Papéis de usuário aceitos pela aplicação. Centraliza a regra de negócio que antes
    /// estava duplicada (e escrita como string solta) em cada Controller.
    /// </summary>
    public static class UserRoles
    {
        public const string Tutor = "Tutor";
        public const string Veterinario = "Veterinario";

        public static readonly IReadOnlyCollection<string> All = new[] { Tutor, Veterinario };

        public static bool IsValid(string? role) =>
            !string.IsNullOrWhiteSpace(role) && All.Contains(role, StringComparer.OrdinalIgnoreCase);

        public static bool IsTutor(string? role) =>
            string.Equals(role, Tutor, StringComparison.OrdinalIgnoreCase);

        public static bool IsVeterinario(string? role) =>
            string.Equals(role, Veterinario, StringComparison.OrdinalIgnoreCase);
    }
}
