using System.Runtime.Serialization;

namespace Domain.Enums;

public enum CashbackCurrency
{
    miles,
    rub,
    [EnumMember(Value = "bravo-points")]
    bravoPoints
}