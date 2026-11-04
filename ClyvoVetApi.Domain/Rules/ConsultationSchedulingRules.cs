namespace ClyvoVetApi.Domain
{
    /// <summary>
    /// Regras de agendamento de consultas: não permitir datas passadas e detectar
    /// conflito de horário na agenda de um mesmo veterinário.
    /// </summary>
    public static class ConsultationSchedulingRules
    {
        public static bool IsFutureOrPresent(DateTime consultationDate, DateTime referenceNow) =>
            consultationDate >= referenceNow;

        /// <summary>
        /// Duas consultas do mesmo veterinário conflitam se a diferença entre os horários
        /// for menor que a janela mínima exigida entre atendimentos.
        /// </summary>
        public static bool HasConflict(DateTime existingDate, DateTime newDate, TimeSpan minimumGap) =>
            (existingDate - newDate).Duration() < minimumGap;
    }
}
