namespace ScanMaster.Database.Objects
{
    public class ResourceItem
    {
        DRBDatabase database;
        public uint ID { get; private set; }
        public string ResourceString { get; private set; }
        public string OBDCode { get; private set; }

        public ResourceItem(DRBDatabase parentDb, uint id, string resourceString, string obdCode)
        {
            database = parentDb;
            ID = id;
            ResourceString = resourceString;
            OBDCode = obdCode;
        }
    }
}
