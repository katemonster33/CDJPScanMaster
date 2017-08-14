using System.Collections.Generic;
using System.Linq;

namespace ScanMaster.Database.Objects
{
    public class DataMenuItem
    {
        DRBDatabase database;
        public uint ID { get; private set; }
        public uint ModuleTypeID { get; private set; }
        public uint NameID { get; private set; }
        public int Order { get; private set; }
        public List<uint> FunctionIDs { get; private set; }
        public List<uint> TXGroupIDs { get; private set; }
        public DataMenuItem(DRBDatabase parentDb, uint id, uint moduleTypeId, uint nameId, int order)
        {
            database = parentDb;
            ID = id;
            ModuleTypeID = moduleTypeId;
            NameID = nameId;
            Order = order;
            FunctionIDs = new List<uint>();
            TXGroupIDs = new List<uint>();
        }

        List<Function> functions = null;
        public List<Function> Functions
        {
            get
            {
                return functions ?? (functions = FunctionIDs.Select(ID => database.GetFunctionByID(ID)).ToList());
            }
        }

        List<TXItemGroup> txGroups = null;
        public List<TXItemGroup> TXGroups
        {
            get
            {
                return txGroups ?? (txGroups = TXGroupIDs.Select(ID => database.GetTXGroup(ID)).ToList());
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
