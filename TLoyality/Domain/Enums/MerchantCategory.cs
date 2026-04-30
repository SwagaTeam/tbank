namespace Domain.Enums;

public enum MerchantCategory
{
    Supermarkets,
    Pharmacy,
    Restaurants,
    GasStations,
    Electronics,
    Clothes
}

public static class MapCategory
{
    public static string MapCategoryToRussian(this MerchantCategory category) => category switch
    {
        MerchantCategory.Supermarkets => "Супермаркеты",
        MerchantCategory.Pharmacy => "Аптеки",
        MerchantCategory.Restaurants => "Рестораны",
        MerchantCategory.GasStations => "АЗС",
        MerchantCategory.Electronics => "Электроника",
        MerchantCategory.Clothes => "Одежда",
        _ => category.ToString()
    };
}
