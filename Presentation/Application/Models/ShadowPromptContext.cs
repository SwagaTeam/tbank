using Domain;
using Domain.Entities;

namespace Application.Models;

internal record ShadowPromptContext(
    User User,
    ICollection<Accounts> CurrentAccount,
    ICollection<LoyaltyHistory> RecentHistory,
    ICollection<LoyaltyPrograms> AvailablePrograms,
    ICollection<Transaction> Transactions,
    ICollection<Offers> RelevantOffers
);