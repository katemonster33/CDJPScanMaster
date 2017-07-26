namespace DRBDB.Objects
{
    public class ModuleMenuItem
    {
        Database database;
        public uint ID { get; private set; }
        public uint NameID { get; private set; }
        public ModuleMenuItem(Database parentDb, uint id, uint nameId)
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
