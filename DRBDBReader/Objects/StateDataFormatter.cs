using ScanMaster.Database.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace ScanMaster.Database.Objects
{
    public class StateDataFormatter : DataFormatter
    {
        DRBDatabase database;
        public uint ID { get; private set; }
        public List<KeyValuePair<uint, uint>> StateIDs { get; private set; }
        public StateDataFormatter(DRBDatabase parentDb, uint id)
        {
            database = parentDb;
            ID = id;
            StateIDs = new List<KeyValuePair<uint, uint>>();
        }

        public string FormatData(DataDisplay container, bool isMetric)
        {
            foreach (KeyValuePair<uint, ResourceItem> state in States)
            {
                if(container.ScaledIntData == state.Key)
                {
                    if (state.Value == null) return "(BARF)";
                    else return state.Value.ResourceString;
                }
            }
            return "N/A";
        }

        public List<KeyValuePair<uint, ResourceItem>> States
        {
            get
            {
                return StateIDs.Select(stateId =>
                    new KeyValuePair<uint, ResourceItem>(stateId.Key, database.GetResource(stateId.Value))).ToList();
            }
        }
    }
}
