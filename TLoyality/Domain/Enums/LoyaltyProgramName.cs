using System.Runtime.Serialization;

namespace Domain.Enums;

public enum LoyaltyProgramName
{
    [EnumMember(Value = "All Airlines")]
    AllAirlines,
    Black,
    Bravo
}