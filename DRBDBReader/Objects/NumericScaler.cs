using DRBDB.Helpers;

namespace DRBDB.Objects
{
    public class NumericScaler : DataScaler
    {
        public uint ID { get; private set; }
        public float Slope { get; private set; }
        public float Offset { get; private set; }
        public NumericScaler(uint id, float slope, float offset)
        {
            ID = id;
            Slope = slope;
            Offset = offset;
        }

        public void ScaleData(DataDisplay dataContainer)
        {
            dataContainer.ScaledFloatData = dataContainer.RawIntData * Slope + Offset;
        }
    }
}
