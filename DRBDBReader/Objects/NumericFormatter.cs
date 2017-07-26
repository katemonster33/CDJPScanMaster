namespace DRBDB.Objects
{
    public class NumericFormatter : DataFormatter
    {
        Database database;
        public uint ID { get; private set; }
        public uint EnglishUnitNameID { get; private set; }
        public uint MetricUnitNameID { get; private set; }
        public float MetricConversionSlope { get; private set; }
        public float MetricConversionOffset { get; private set; }
        public NumericFormatter(Database parentDb, uint id, uint englishUnitNameId, uint metricUnitNameId, float metricConvSlope, float metricConvOffset)
        {
            database = parentDb;
            ID = id;
            EnglishUnitNameID = englishUnitNameId;
            MetricUnitNameID = metricUnitNameId;
            MetricConversionSlope = metricConvSlope;
            MetricConversionOffset = metricConvOffset;
        }
        ResourceItem englishUnitName = null;
        public ResourceItem EnglishUnitName
        {
            get
            {
                return englishUnitName ?? (englishUnitName = database.GetResource(EnglishUnitNameID));
            }
        }

        ResourceItem metricUnitName = null;
        public ResourceItem MetricUnitName
        {
            get
            {
                return metricUnitName ?? (metricUnitName = database.GetResource(MetricUnitNameID));
            }
        }

        public string FormatData(float inputData, bool isMetric)
        {
            if (isMetric) inputData = inputData * MetricConversionSlope + MetricConversionOffset;
            string formatted = inputData.ToString();
            if (isMetric)
            {
                if (MetricUnitName != null) formatted += " " + MetricUnitName.ResourceString;
            }
            else
            {
                if (EnglishUnitName != null) formatted += " " + EnglishUnitName.ResourceString;
            }
            return formatted;
        }
    }
}
