using Domain;

namespace Application.Models;

internal record ShadowPromptContext(
    User User,
    ICollection<Accounts> CurrentAccount,
    ICollection<LoyaltyHistory> RecentHistory,
    ICollection<LoyaltyPrograms> AvailablePrograms,
    ICollection<Offers> RelevantOffers
);