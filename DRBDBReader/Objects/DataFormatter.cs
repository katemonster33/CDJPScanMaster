using ScanMaster.Database.Helpers;

namespace ScanMaster.Database.Objects
{
    public interface DataFormatter
    {
        string FormatData(DataDisplay container, bool isMetric);
    }
}
