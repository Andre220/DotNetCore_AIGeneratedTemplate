using TemplateApi.Application.Common.Interfaces;

namespace TemplateApi.Infrastructure.Services;

/// <summary>
/// Implementação concreta do DateTimeService
/// Em produção, retorna DateTime real
/// Em testes, pode ser mockado para controlar o tempo
/// </summary>
public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
}
