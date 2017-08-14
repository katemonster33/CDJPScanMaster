using ScanMaster.Database.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanMaster.Database.Helpers
{
    public class DataDisplay
    {
        DRBDatabase database;
        public byte ConversionScalingType { get; private set; }
        public byte ConversionFormatterType { get; private set; }
        public uint ConversionScalingID { get; private set; }
        public uint ConversionFormatterID { get; private set; }

        DataScaler scaler = null;
        public DataScaler DataScaler
        {
            get
            {
                if (scaler == null)
                {
                    switch (ConversionScalingType)
                    {
                        case 0:
                            scaler = database.GetStateScaler(ConversionScalingID);
                            break;
                        case 1:
                            scaler = database.GetNumericScaler(ConversionScalingID);
                            break;
                        case 2:
                            break;
                    }
                }
                return scaler;
            }
        }

        DataFormatter formatter = null;
        public DataFormatter DataFormatter
        {
            get
            {
                if (formatter == null)
                {
                    switch (ConversionFormatterType)
                    {
                        case 0:
                            formatter = database.GetBinaryFormatter(ConversionFormatterID);
                            break;
                        case 1:
                            formatter = database.GetNumericFormatter(ConversionFormatterID);
                            break;
                        case 2:
                            formatter = database.GetStateFormatter(ConversionFormatterID);
                            break;
                    }
                }
                return formatter;
            }
        }
        
        byte[] rawData = null;
        public byte[] RawData
        {
            get
            {
                return rawData;
            }
            set
            {
                if (value != null)
                {
                    if (rawData == null || rawData.Length != value.Length)
                    {
                        rawData = value;
                        isRawDataUpdated = true;
                        RawDataUpdated?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        for (int i = 0; i < rawData.Length; i++)
                        {
                            if (rawData[i] != value[i])
                            {
                                isRawDataUpdated = true;
                                break;
                            }
                        }
                        if (isRawDataUpdated)
                        {
                            rawData = value;
                            RawDataUpdated?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
        }

        bool isRawDataUpdated = false;
        public bool IsRawDataUpdated
        {
            get
            {
                return isRawDataUpdated;
            }
        }

        public int RawIntData
        {
            get
            {
                if (rawData.Length == 1) return rawData[0];
                else
                {
                    int output = 0;
                    if (database.isStarScanDB)
                    {
                        for (int i = rawData.Length - 1, shift = 0; i > 0; i--, shift += 8)
                        {
                            output |= rawData[i] << shift;
                        }
                    }
                    else
                    {
                        for (int i = 0, shift = 0; i < rawData.Length; i++, shift += 8)
                        {
                            output |= rawData[i] << shift;
                        }
                    }
                    return output;
                }
            }
        }

        public int ScaledIntData = 0;
        public float? ScaledFloatData = null;

        string scaleAndFormatData()
        {
            if (DataScaler != null) DataScaler.ScaleData(this);
            if(DataFormatter != null)
            {
                return DataFormatter.FormatData(this, false);
            }
            else
            {
                if (ScaledFloatData != null) return ScaledFloatData.GetValueOrDefault().ToString();
                else return ScaledIntData.ToString();
            }
        }

        string formattedData = "N/A";
        public string FormattedData
        {
            get
            {
                if(isRawDataUpdated)
                {
                    formattedData = scaleAndFormatData();
                    isRawDataUpdated = false;
                }
                return formattedData;
            }
        }

        public event EventHandler RawDataUpdated;
        public DataDisplay(DRBDatabase database, byte[] conversionBytes)
        {
            this.database = database;
            ConversionScalingType = (byte)(conversionBytes[0] & 0x0F);
            ConversionFormatterType = (byte)(conversionBytes[0] >> 4);
            ConversionFormatterID = BitConverter.ToUInt16(conversionBytes, 2);
            ConversionScalingID = BitConverter.ToUInt16(conversionBytes, 4);
        }
    }
}
