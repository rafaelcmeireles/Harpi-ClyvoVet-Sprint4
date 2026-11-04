namespace ClyvoVetApi.Domain
{
    /// <summary>
    /// Regras de validação específicas de Usuário. Ex.: veterinários precisam de CRMV.
    /// </summary>
    public static class UserValidationRules
    {
        public static bool RequiresCrmv(string role) => UserRoles.IsVeterinario(role);

        public static bool HasValidCrmv(string role, string? crmv)
        {
            if (!RequiresCrmv(role))
                return true;

            return !string.IsNullOrWhiteSpace(crmv);
        }
    }
}
