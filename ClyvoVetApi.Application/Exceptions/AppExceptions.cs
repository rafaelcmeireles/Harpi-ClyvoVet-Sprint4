namespace ClyvoVetApi.Application.Exceptions
{
    /// <summary>
    /// Lançada quando uma entidade solicitada não é encontrada. Convertida pelo
    /// middleware global de exceções em HTTP 404.
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    /// <summary>
    /// Lançada quando uma regra de negócio é violada. Convertida pelo middleware
    /// global de exceções em HTTP 400.
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message) { }
    }

    /// <summary>
    /// Lançada quando as credenciais são inválidas ou o usuário não tem permissão para a
    /// operação. Convertida pelo middleware global em HTTP 401/403.
    /// </summary>
    public class UnauthorizedAppException : Exception
    {
        public UnauthorizedAppException(string message) : base(message) { }
    }

    public class ForbiddenAppException : Exception
    {
        public ForbiddenAppException(string message) : base(message) { }
    }
}