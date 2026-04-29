using Domain;
using Domain.Entities;

namespace Application.Models;

/// <summary>
/// Объект контекста, передаваемый в LLM для генерации 
/// персонализированных рекомендаций и анализа финансового поведения.
/// </summary>
/// <param name="User">Данные профиля пользователя (включая ФИО и финансовый сегмент).</param>
/// <param name="CurrentAccount">Список активных счетов пользователя с текущими балансами кешбэка.</param>
/// <param name="RecentHistory">История последних выплат кешбэка для анализа динамики начислений.</param>
/// <param name="AvailablePrograms">Список всех доступных программ лояльности банка для поиска более выгодных условий.</param>
/// <param name="Transactions">
/// История реальных транзакций (категории, суммы, даты). 
/// Ключевой параметр для определения паттернов трат и скрытой выгоды.
/// </param>
/// <param name="RelevantOffers">Список актуальных предложений партнеров, отфильтрованный под сегмент пользователя.</param>
internal record ShadowPromptContext(
    User User,
    ICollection<Accounts> CurrentAccount,
    ICollection<LoyaltyHistory> RecentHistory,
    ICollection<LoyaltyPrograms> AvailablePrograms,
    ICollection<Transaction> Transactions,
    ICollection<Offers> RelevantOffers);