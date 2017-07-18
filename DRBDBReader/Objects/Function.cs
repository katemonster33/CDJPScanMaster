using System.Collections.Generic;
using System.Linq;

namespace DRBDB.Objects
{
    public class Function : SelectableDataItem
    {
        public uint Order { get; private set; }
        public List<uint> LinkedFunctionIDs { get; private set; }
        public Function(Database parentDb, uint id, uint moduleTypeId, uint nameId, uint moduleMenuId, uint order)
        {
            database = parentDb;
            ID = id;
            NameID = nameId;
            ModuleMenuID = moduleMenuId;
            Order = order;
            LinkedFunctionIDs = new List<uint>();
        }

        List<Function> linkedFunctions = null;
        public List<Function> LinkedFunctions
        {
            get
            {
                return linkedFunctions ?? (linkedFunctions = LinkedFunctionIDs.Select(ID => database.GetFunctionByID(ID)).ToList());
            }
        }
    }
}
