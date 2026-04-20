namespace Orders.API.Application.Behaviors;

/// <summary>
/// Marker interface para Commands que necesitan transacción de base de datos.
/// Las Queries NO implementan esta interfaz — no necesitan transacción.
/// TransactionBehavior solo se activa para tipos que implementan ITransactional.
/// </summary>
public interface ITransactional { }
