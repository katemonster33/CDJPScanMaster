using DRBDB.Helpers;
using System;

namespace DRBDB.Objects
{
    public class TXItem : SelectableDataItem
    {
        public uint FunctionID { get; private set; }
        public uint DataAcquisitionID { get; private set; }
        public byte[] TransmitBytes { get; private set; }
        public uint HintID { get; private set; }
        public DataDisplay DataDisplay { get; set; }
        public TXItem(Database parentDb, uint id, byte[] conversionBytes, uint dataAcquireId, uint moduleMenuId, uint functionId, byte[] xmitBytesRaw, uint nameId, uint hintId, uint moduleTypeId)
        {
            database = parentDb;
            ID = id;
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
            DataDisplay = new DataDisplay(parentDb, conversionBytes);
        }


        ResourceItem hint = null;
        public ResourceItem Hint
        {
            get
            {
                return hint ?? (hint = database.GetResource(HintID));
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
