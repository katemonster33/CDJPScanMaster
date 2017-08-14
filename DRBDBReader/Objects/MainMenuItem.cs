using System.Collections.Generic;

namespace ScanMaster.Database.Objects
{
    public class MainMenuItem
    {
        DRBDatabase database;
        public uint ID { get; private set; }
        public uint ParentID { get; private set; }
        public uint NameID { get; private set; }
        public int Order { get; private set; }
        public int ScreenYPosition { get; private set; }
        public MainMenuItem(DRBDatabase parentDb, uint id, uint parentId, uint nameId, int order, int screenYPosition)
        {
            database = parentDb;
            ID = id;
            ParentID = parentId;
            NameID = nameId;
            Order = order;
            ScreenYPosition = screenYPosition;
        }

        List<MainMenuItem> children = null;
        public List<MainMenuItem> Children
        {
            get
            {
                return children ?? (children = database.GetMenuChildren(ID));
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
