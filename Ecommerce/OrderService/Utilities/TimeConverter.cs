using Google.Protobuf.WellKnownTypes;
using Google.Type;
using static Azure.Core.HttpHeader;

namespace OrderService.Utilities
{
    internal class TimeConverter
    {
        const int NanoСoefficient = 1000000;

        internal static Timestamp ConvertDateTimeToTimeStapm (DateTime dateTime)
        {
            Timestamp timestamp = new Timestamp();

            timestamp.Seconds = new DateTimeOffset(dateTime).ToUnixTimeSeconds();
            timestamp.Nanos = dateTime.Millisecond * NanoСoefficient;

            return timestamp;
        }
    }
}