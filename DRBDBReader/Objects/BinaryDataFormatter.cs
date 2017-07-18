using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRBDB.Objects
{
    public class BinaryDataFormatter : DataFormatter
    {
        Database database;
        public uint ID { get; private set; }
        public uint FalseStringID { get; private set; }
        public uint TrueStringID { get; private set; }

        public BinaryDataFormatter(Database parentDb, uint id, uint falseStringId, uint trueStringId)
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

        public string FormatData(float inputData, bool isMetric)
        {
            if (FalseString == null || TrueString == null) return "(BARF)";
            return (inputData != 0 ? TrueString.ResourceString : FalseString.ResourceString);
        }
    }
}
