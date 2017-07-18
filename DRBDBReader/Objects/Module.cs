using System.Collections.Generic;
using System.Linq;

namespace DRBDB.Objects
{
    public class Module
    {
        Database database;
        public uint ID { get; private set; }
        public uint ModuleTypeID { get; private set; }
        public uint NameID { get; private set; }
        public List<uint> TXItemIDs { get; internal set; }
        public List<uint> DataMenuItemIDs { get; private set; }

        public Module(Database parentDb, uint id, uint moduleTypeID, uint stringID)
        {
            database = parentDb;
            ID = id;
            ModuleTypeID = moduleTypeID;
            NameID = stringID;
            TXItemIDs = new List<uint>();
            DataMenuItemIDs = new List<uint>();
        }
        List<TXItem> txItems = null;
        public List<TXItem> TXItems
        {
            get
            {
                return txItems ?? (txItems = TXItemIDs.Select(ID => database.GetTXItem(ID)).ToList());
            }
        }

        List<DataMenuItem> dataMenuItems = null;
        public List<DataMenuItem> DataMenuItems
        {
            get
            {
                return dataMenuItems ?? (dataMenuItems = DataMenuItemIDs.Select(ID => database.GetDataMenu(ID)).ToList());
            }
        }
        ModuleType moduleType = null;
        public ModuleType ModuleType
        {
            get
            {
                return moduleType ?? (moduleType = database.GetModuleType(ModuleTypeID));
            }
        }
        ResourceItem name = null;
        public ResourceItem Name
        {
            get
            {
                return name ?? (name = database.GetResource(NameID));
            }
        }
    }
}
