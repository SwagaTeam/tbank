using System.Runtime.Serialization;

namespace Domain;

public enum LoyaltyProgramName
{
    [EnumMember(Value = "All Airlines")]
    AllAirlines,
    Black,
    Bravo
}