namespace ClyvoVetApi.Domain
{
    /// <summary>
    /// Regras de validação de dados de um Pet.
    /// </summary>
    public static class PetValidationRules
    {
        /// <summary>
        /// A data de nascimento, se informada, não pode estar no futuro em relação à data de referência.
        /// </summary>
        public static bool IsBirthDateValid(DateTime? birthDate, DateTime referenceNow) =>
            birthDate == null || birthDate.Value.Date <= referenceNow.Date;

        /// <summary>
        /// O peso, se informado, precisa ser maior que zero.
        /// </summary>
        public static bool IsWeightValid(double? weightKg) =>
            weightKg == null || weightKg.Value > 0;
    }
}
