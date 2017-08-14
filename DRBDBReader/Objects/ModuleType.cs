namespace ScanMaster.Database.Objects
{
    public class ModuleType
    {
        DRBDatabase database;
        public uint ID { get; private set; }
        public uint TypeID { get; private set; }
        public uint NameID { get; private set; }
        public ModuleType(DRBDatabase parentDb, uint id, uint typeId, uint nameId)
        {
            database = parentDb;
            ID = id;
            TypeID = typeId;
            NameID = nameId;
        }

        ResourceItem name = null;
        public ResourceItem Name
        {
            get
            {
                return name ?? (name = database.GetResource(NameID));
            }
        }

        public override string ToString()
        {
            return Name != null ? Name.ResourceString : ID.ToString();
        }
    }
}
