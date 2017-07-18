using System;

namespace DRBDB.Objects
{
    public class TXItem : SelectableDataItem
    {
        public byte ConversionScalingType { get; private set; }
        public byte ConversionFormatterType { get; private set; }
        public uint ConversionScalingID { get; private set; }
        public uint ConversionFormatterID { get; private set; }
        public uint FunctionID { get; private set; }
        public uint DataAcquisitionID { get; private set; }
        public byte[] TransmitBytes { get; private set; }
        public uint HintID { get; private set; }
        public TXItem(Database parentDb, uint id, byte[] conversionBytes, uint dataAcquireId, uint moduleMenuId, uint functionId, byte[] xmitBytesRaw, uint nameId, uint hintId, uint moduleTypeId)
        {
            database = parentDb;
            ID = id;
            ConversionScalingType = (byte)(conversionBytes[0] & 0x0F);
            ConversionFormatterType = (byte)(conversionBytes[0] >> 4);
            ConversionFormatterID = BitConverter.ToUInt16(conversionBytes, 2);
            ConversionScalingID = BitConverter.ToUInt16(conversionBytes, 4);
            DataAcquisitionID = dataAcquireId;
            FunctionID = functionId;
            TransmitBytes = new byte[xmitBytesRaw[0]];
            ModuleMenuID = moduleMenuId;
            if(xmitBytesRaw[0] > 0)
            {
                Buffer.BlockCopy(xmitBytesRaw, 1, TransmitBytes, 0, TransmitBytes.Length);
            }
            NameID = nameId;
            ModuleTypeID = moduleTypeId;
        }

        ResourceItem hint = null;
        public ResourceItem Hint
        {
            get
            {
                return hint ?? (hint = database.GetResource(HintID));
            }
        }

        DataScaler scaler = null;
        public DataScaler DataScaler
        {
            get
            {
                if (scaler == null)
                {
                    switch(ConversionScalingType)
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
                if(formatter == null)
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

        DataAcquisitionMethod dataAcquirer = null;
        public DataAcquisitionMethod DataAcquisitionMethod
        {
            get
            {
                return dataAcquirer ?? (dataAcquirer = database.GetDataAcquirer(DataAcquisitionID));
            }
        }

        Function function = null;
        public Function Function
        {
            get
            {
                return function ?? (function = database.GetFunctionByID(FunctionID));
            }
        }
    }
}
