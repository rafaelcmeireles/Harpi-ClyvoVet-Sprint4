namespace ClyvoVetApi.Application.Abstractions
{
    /// <summary>
    /// Abstração sobre o relógio do sistema. Permite que os testes unitários fixem
    /// a data/hora "atual" ao mockar esta interface, tornando os testes determinísticos.
    /// </summary>
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }

    public class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
