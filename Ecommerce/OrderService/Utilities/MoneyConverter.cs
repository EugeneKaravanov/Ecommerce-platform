using Google.Type;

namespace OrderService.Utilities
{
    internal class MoneyConverter
    {
        const int NanoСoefficient = 1000000000;

        internal static Money ConvertDecimalToMoney(decimal value)
        {
            Money money = new Money();

            money.Units = (long)value;
            money.Nanos = (int)(value % 1 * NanoСoefficient);
            money.CurrencyCode = "RUB";

            return money;
        }

        internal static decimal ConvertMoneyToDecimal(Money money)
        {
            decimal price = money.Units + (decimal)money.Nanos / NanoСoefficient;

            return price;
        }
    }
}