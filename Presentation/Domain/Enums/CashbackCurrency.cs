using System.Runtime.Serialization;

namespace Domain;

public enum CashbackCurrency
{
    miles,
    rub,
    [EnumMember(Value = "bravo-points")]
    bravoPoints
}