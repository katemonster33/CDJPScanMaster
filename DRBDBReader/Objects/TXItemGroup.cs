using System.Collections.Generic;
using System.Linq;

namespace DRBDB.Objects
{
    public class TXItemGroup
    {
        Database database;
        public uint ID { get; private set; }
        public uint ModuleTypeID { get; private set; }
        public uint Order { get; private set; }
        public List<uint> TXItemIDs { get; internal set; }
        public TXItemGroup(Database parentDb, uint id, uint moduleTypeId, uint order)
        {
            database = parentDb;
            ID = id;
            ModuleTypeID = moduleTypeId;
            Order = order;
            TXItemIDs = new List<uint>();
        }

        ModuleType moduleType = null;
        public ModuleType ModuleType
        {
            get
            {
                return moduleType ?? (moduleType = database.GetModuleType(ModuleTypeID));
            }
        }

        List<TXItem> txItems = null;
        public List<TXItem> TXItems
        {
            get
            {
                return txItems ?? (txItems = TXItemIDs.Select(ID => database.GetTXItem(ID)).ToList());
            }
        }

    }
}
