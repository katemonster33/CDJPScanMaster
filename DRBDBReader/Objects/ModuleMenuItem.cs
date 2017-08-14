namespace ScanMaster.Database.Objects
{
    public class ModuleMenuItem
    {
        DRBDatabase database;
        public uint ID { get; private set; }
        public uint NameID { get; private set; }
        public ModuleMenuItem(DRBDatabase parentDb, uint id, uint nameId)
        {
            database = parentDb;
            ID = id;
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
