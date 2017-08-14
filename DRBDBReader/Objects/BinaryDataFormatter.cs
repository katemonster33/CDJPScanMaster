using ScanMaster.Database.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanMaster.Database.Objects
{
    public class BinaryDataFormatter : DataFormatter
    {
        DRBDatabase database;
        public uint ID { get; private set; }
        public uint FalseStringID { get; private set; }
        public uint TrueStringID { get; private set; }

        public BinaryDataFormatter(DRBDatabase parentDb, uint id, uint falseStringId, uint trueStringId)
        {
            database = parentDb;
            ID = id;
            FalseStringID = falseStringId;
            TrueStringID = trueStringId;
        }

        ResourceItem falseString = null;
        public ResourceItem FalseString
        {
            get
            {
                return falseString ?? (falseString = database.GetResource(FalseStringID));
            }
        }

        ResourceItem trueString = null;
        public ResourceItem TrueString
        {
            get
            {
                return trueString ?? (trueString = database.GetResource(TrueStringID));
            }
        }

        public string FormatData(DataDisplay container, bool isMetric)
        {
            if (FalseString == null || TrueString == null) return "(BARF)";
            return (container.ScaledIntData != 0 ? TrueString.ResourceString : FalseString.ResourceString);
        }
    }
}
