using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRBDB.Objects
{
    public abstract class SelectableDataItem
    {
        protected Database database = null;
        public uint ID { get; protected set; }
        public uint NameID { get; protected set; }
        public uint ModuleTypeID { get; protected set; }
        public uint ModuleMenuID { get; protected set; }


        ResourceItem name = null;
        public ResourceItem Name
        {
            get
            {
                return name ?? (name = database.GetResource(NameID));
            }
        }

        ModuleMenuItem moduleMenu = null;
        public ModuleMenuItem ModuleMenu
        {
            get
            {
                return moduleMenu ?? (moduleMenu = database.GetModuleMenu(ModuleMenuID));
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

        public string GetCaption()
        {
            return (Name != null ? Name.ResourceString : "(BARF)");
        }
    }
}
