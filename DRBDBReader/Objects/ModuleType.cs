namespace DRBDB.Objects
{
    public class ModuleType
    {
        Database database;
        public uint ID { get; private set; }
        public uint TypeID { get; private set; }
        public uint NameID { get; private set; }
        public ModuleType(Database parentDb, uint id, uint typeId, uint nameId)
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
            return name != null ? name.ResourceString : ID.ToString();
        }
    }
}
