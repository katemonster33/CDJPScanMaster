using DRBDB.Helpers;

namespace DRBDB.Objects
{
    public interface DataFormatter
    {
        string FormatData(DataDisplay container, bool isMetric);
    }
}
