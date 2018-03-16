/*
 * DRBDBReader
 * Copyright (C) 2016-2017, Katie McKean
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text;
using ScanMaster.Database.Objects;

namespace ScanMaster.Database
{
    public class DRBDatabase
    {
        FileInfo dbFile;
        public SimpleBinaryReader dbReader;
        public Table[] tables;

        Dictionary<uint, ResourceItem> Resources = new Dictionary<uint, ResourceItem>();
        Dictionary<uint, TXItem> TXItems = new Dictionary<uint, TXItem>();
        Dictionary<uint, TXItemGroup> TXGroups = new Dictionary<uint, TXItemGroup>();
        Dictionary<uint, MainMenuItem> MainMenuItems = new Dictionary<uint, MainMenuItem>();
        Dictionary<uint, ModuleMenuItem> ModuleMenuItems = new Dictionary<uint, ModuleMenuItem>();
        Dictionary<uint, DataMenuItem> DataMenuItems = new Dictionary<uint, DataMenuItem>();
        Dictionary<uint, BinaryDataFormatter> BinaryFormatters = new Dictionary<uint, BinaryDataFormatter>();
        Dictionary<uint, DataAcquisitionMethod> DataAcquireMethods = new Dictionary<uint, DataAcquisitionMethod>();
        Dictionary<uint, Function> Functions = new Dictionary<uint, Function>();
        Dictionary<uint, Module> Modules = new Dictionary<uint, Module>();
        Dictionary<uint, ModuleType> ModuleTypes = new Dictionary<uint, ModuleType>();
        Dictionary<uint, NumericFormatter> NumericFormatters = new Dictionary<uint, NumericFormatter>();
        Dictionary<uint, NumericScaler> NumericScalers = new Dictionary<uint, NumericScaler>();
        Dictionary<uint, StateDataFormatter> StateFormatters = new Dictionary<uint, StateDataFormatter>();
        Dictionary<uint, StateDataScaler> StateScalers = new Dictionary<uint, StateDataScaler>();

        public bool isStarScanDB;

        public DRBDatabase(FileInfo dbFile)
        {
            this.dbFile = dbFile;

            /* Since we're going to need access to this data often, lets load it into a MemoryStream.
			 * With it being about 2.5MB it's fairly cheap.
			 */
            using (FileStream fs = new FileStream(this.dbFile.FullName, FileMode.Open, FileAccess.Read))
            {
                this.dbReader = new SimpleBinaryReader(fs);
            }

            /* StarSCAN's database.mem has a different endianness;
			 * This detects and accounts for that as needed.
			 */
            this.isStarScanDB = this.checkStarScan();

            this.makeTables();
        }
        public DRBDatabase()
        {
            this.dbFile = new FileInfo("database.mem");

            /* Since we're going to need access to this data often, lets load it into a MemoryStream.
			 * With it being about 2.5MB it's fairly cheap.
			 */
            this.dbReader = new SimpleBinaryReader(new FileStream("database.mem", FileMode.Open));

            /* StarSCAN's database.mem has a different endianness;
			 * This detects and accounts for that as needed.
			 */
            this.isStarScanDB = this.checkStarScan();

            this.makeTables();
        }

        internal Function GetFunctionByID(uint ID)
        {
            return Functions.GetValueOrNull(ID);
        }

        public Module GetModuleByName(string name)
        {
            return Modules.Values.FirstOrDefault(mod => mod.Name != null && mod.Name.ResourceString == name);
        }

        internal List<MainMenuItem> GetMenuChildren(uint ID)
        {
            return MainMenuItems.Values.Where(item => item.ParentID == ID).ToList();
        }

        internal NumericFormatter GetNumericFormatter(uint ID)
        {
            return NumericFormatters.GetValueOrNull(ID);
        }

        internal NumericScaler GetNumericScaler(uint ID)
        {
            return NumericScalers.GetValueOrNull(ID);
        }

        internal BinaryDataFormatter GetBinaryFormatter(uint ID)
        {
            return BinaryFormatters.GetValueOrNull(ID);
        }

        internal StateDataScaler GetStateScaler(uint ID)
        {
            return StateScalers.GetValueOrNull(ID);
        }

        internal StateDataFormatter GetStateFormatter(uint ID)
        {
            return StateFormatters.GetValueOrNull(ID);
        }

        public List<ModuleMenuItem> GetModuleMenusWithChildren(Module module)
        {
            List<ModuleMenuItem> output = new List<ModuleMenuItem>();
            List<Function> modulesFunctions = GetModuleFunctionsWithoutTX(module);
            foreach (ModuleMenuItem moduleMenu in ModuleMenuItems.Values)
            {
                IEnumerable<TXItem> txItemsToShow = module.TXItems.Where(item => item.ModuleMenuID == moduleMenu.ID);
                IEnumerable<Function> functionsToShow = modulesFunctions.Where(func => func.ModuleMenuID == moduleMenu.ID);
                if(txItemsToShow.Any() || functionsToShow.Any())
                {
                    output.Add(moduleMenu);
                }
            }
            return output;
        }

        public List<SelectableDataItem> GetTXChildren(Module mod, Function func)
        {
            List<SelectableDataItem> output = new List<SelectableDataItem>();
            foreach(Function child in func.LinkedFunctions)
            {
                TXItem linkedTx = mod.TXItems.FirstOrDefault(item => item.FunctionID == child.ID);
                if (linkedTx != null) output.Add(linkedTx);
                else output.Add(child);
            }
            return output;
        }

        public List<Function> GetModuleFunctionsWithoutTX(Module module) // this function returns functions that don't have associated TX items
        {
            List<Function> modulesFunctions = module.TXItems.
                            Select(tx => tx.Function).
                            Where(func => func != null).
                            Distinct().
                            ToList();
            modulesFunctions = modulesFunctions.
                Union(modulesFunctions.
                    SelectMany(func => func.LinkedFunctions)).
                Except(module.TXItems.
                    Select(tx => tx.Function).
                    Where(func => func != null)).
                Distinct().
                ToList();
            return modulesFunctions;
        }

        internal DataAcquisitionMethod GetDataAcquirer(uint ID)
        {
            return DataAcquireMethods.GetValueOrNull(ID);
        }

        internal ResourceItem GetResource(uint ID)
        {
            return Resources.GetValueOrNull(ID);
        }

        public Module GetModule(uint ID)
        {
            return Modules.GetValueOrNull(ID);
        }

        internal TXItem GetTXItem(uint ID)
        {
            return TXItems.GetValueOrNull(ID);
        }

        public List<TXItem> GetTXItems()
        {
            return TXItems.Values.ToList();
        }

        internal TXItemGroup GetTXGroup(uint ID)
        {
            return TXGroups.GetValueOrNull(ID);
        }

        internal ModuleType GetModuleType(uint ID)
        {
            return ModuleTypes.GetValueOrNull(ID);
        }

        internal ModuleMenuItem GetModuleMenu(uint ID)
        {
            return ModuleMenuItems.GetValueOrNull(ID);
        }

        internal DataMenuItem GetDataMenu(uint ID)
        {
            return DataMenuItems.GetValueOrNull(ID);
        }

        bool checkStarScan()
        {
            int offset = this.dbReader.rawDB.Length - 0x17;
            return Encoding.ASCII.GetString(this.dbReader.ReadBytes(ref offset, 8)) == "STARSCAN";
        }

        void makeTables()
        {
            int readOffset = 0;
            uint fileSize = this.dbReader.ReadUInt32(ref readOffset);
            this.dbReader.ReadUInt16(ref readOffset);
            ushort numTables = this.dbReader.ReadUInt16(ref readOffset);
            this.tables = new Table[numTables];
            uint tableOffset = 0;
            ushort rowCount = 0, rowSize = 0;
            List<byte> colSizes = new List<byte>();
            for (ushort i = 0; i < numTables; i++)
            {
                ReadTableAttributes(dbReader, ref readOffset, out tableOffset, out rowCount, out rowSize, out colSizes);
                this.tables[i] = new Table(this, i, tableOffset, rowCount, rowSize, colSizes);
            }
            ReadModules(tables[0], tables[10], tables[19]);
            ReadModuleMenuItems(tables[1]);
            ReadBinaryFormatters(tables[2]);
            ReadFunctions(tables[3], tables[11]);
            ReadStateScalers(tables[4]);
            ReadNumericScalers(tables[5]);
            ReadModuleTypes(tables[6]);
            ReadTXGroups(tables[7], tables[18]);
            ReadDataAcquisitions(tables[8]);
            ReadMainMenuItems(tables[9]);
            ReadStateFormatters(tables[13], tables[15]);
            ReadStrings(tables[16], tables[26], tables[27]);
            ReadNumericFormatters(tables[17]);
            ReadDataMenus(tables[21], tables[20], tables[22]);
            ReadTXItems(tables[23]);
        }

        public List<Module> GetModules()
        {
            return Modules.Values.ToList();
        }

        public List<ModuleType> GetModuleTypes()
        {
            return ModuleTypes.Values.ToList();
        }

        public List<ModuleMenuItem> GetModuleMenuItems()
        {
            return ModuleMenuItems.Values.ToList();
        }

        //Modules table columns - 
        // //0 - ID
        // //1 - Module Type ID
        // //2 - Flag?
        // //3 - Resource ID

        //Modules_TXItems table columns - 
        // //0 - Module ID
        // //1 - TXID
        void ReadModules(Table modulesTable, Table moduleTxTable, Table moduleDataMenus)
        {
            for (int i = 0, readOffset = (int)modulesTable.offset; i < modulesTable.rowCount; i++, readOffset += modulesTable.rowSize)
            {
                uint ID = (uint)modulesTable.ReadField(dbReader.rawDB, readOffset, 0);
                uint moduleType = (uint)modulesTable.ReadField(dbReader.rawDB, readOffset, 1);
                uint stringId = (uint)modulesTable.ReadField(dbReader.rawDB, readOffset, 3);
                Modules[ID] = new Module(this, ID, moduleType, stringId);
            }
            for (int i = 0, readOffset = (int)moduleTxTable.offset; i < moduleTxTable.rowCount; i++, readOffset += moduleTxTable.rowSize)
            {
                uint ID = (uint)moduleTxTable.ReadField(dbReader.rawDB, readOffset, 0);
                uint TXID = (uint)moduleTxTable.ReadField(dbReader.rawDB, readOffset, 1);
                Module modItem = null;
                if (Modules.TryGetValue(ID, out modItem))
                {
                    modItem.TXItemIDs.Add(TXID);
                }
            }
            for (int i = 0, readOffset = (int)moduleDataMenus.offset; i < moduleDataMenus.rowCount; i++, readOffset += moduleDataMenus.rowSize)
            {
                uint ID = (uint)moduleDataMenus.ReadField(dbReader.rawDB, readOffset, 0);
                uint dataMenuId = (uint)moduleDataMenus.ReadField(dbReader.rawDB, readOffset, 1);
                Module modItem = null;
                if (Modules.TryGetValue(ID, out modItem))
                {
                    modItem.DataMenuItemIDs.Add(dataMenuId);
                }
            }
        }

        void ReadStateScalers(Table stateScalersTable)
        {
            for (int i = 0, readOffset = (int)stateScalersTable.offset; i < stateScalersTable.rowCount; i++, readOffset += stateScalersTable.rowSize)
            {
                uint ID = (uint)stateScalersTable.ReadField(dbReader.rawDB, readOffset, 0);
                uint mask = (uint)stateScalersTable.ReadField(dbReader.rawDB, readOffset, 1);
                byte op = (byte)stateScalersTable.ReadField(dbReader.rawDB, readOffset, 2);
                StateScalers[ID] = new StateDataScaler(ID, mask, op);
            }
        }

        void ReadDataMenus(Table dataMenuTable, Table dataMenuTxGroups, Table dataMenuFunctions)
        {
            for (int i = 0, readOffset = (int)dataMenuTable.offset; i < dataMenuTable.rowCount; i++, readOffset += dataMenuTable.rowSize)
            {
                uint id = (uint)dataMenuTable.ReadField(dbReader.rawDB, readOffset, 0);
                uint moduleType = (uint)dataMenuTable.ReadField(dbReader.rawDB, readOffset, 1);
                uint stringId = (uint)dataMenuTable.ReadField(dbReader.rawDB, readOffset, 2);
                int order = (int)dataMenuTable.ReadField(dbReader.rawDB, readOffset, 3);
                DataMenuItems[id] = new DataMenuItem(this, id, moduleType, stringId, order);
            }
            for (int i = 0, readOffset = (int)dataMenuTxGroups.offset; i < dataMenuTxGroups.rowCount; i++, readOffset += dataMenuTxGroups.rowSize)
            {
                uint id = (uint)dataMenuTxGroups.ReadField(dbReader.rawDB, readOffset, 0);
                uint groupId = (uint)dataMenuTxGroups.ReadField(dbReader.rawDB, readOffset, 1);
                DataMenuItem menu = null;
                if (DataMenuItems.TryGetValue(id, out menu))
                {
                    menu.TXGroupIDs.Add(groupId);
                }
            }
            for (int i = 0, readOffset = (int)dataMenuFunctions.offset; i < dataMenuFunctions.rowCount; i++, readOffset += dataMenuFunctions.rowSize)
            {
                uint id = (uint)dataMenuFunctions.ReadField(dbReader.rawDB, readOffset, 0);
                uint functionId = (uint)dataMenuFunctions.ReadField(dbReader.rawDB, readOffset, 1);
                DataMenuItem menu = null;
                if (DataMenuItems.TryGetValue(id, out menu))
                {
                    menu.FunctionIDs.Add(functionId);
                }
            }
        }

        void ReadModuleMenuItems(Table moduleMenuTable)
        {
            for (int i = 0, readOffset = (int)moduleMenuTable.offset; i < moduleMenuTable.rowCount; i++, readOffset += moduleMenuTable.rowSize)
            {
                uint ID = (uint)moduleMenuTable.ReadField(dbReader.rawDB, readOffset, 0);
                uint nameId = (uint)moduleMenuTable.ReadField(dbReader.rawDB, readOffset, 1);
                ModuleMenuItems[ID] = new ModuleMenuItem(this, ID, nameId);
            }
        }

        void ReadBinaryFormatters(Table binaryFormattersTable)
        {
            for (int i = 0, readOffset = (int)binaryFormattersTable.offset; i < binaryFormattersTable.rowCount; i++, readOffset += binaryFormattersTable.rowSize)
            {
                uint ID = (uint)binaryFormattersTable.ReadField(dbReader.rawDB, readOffset, 0);
                uint trueStringId = (uint)binaryFormattersTable.ReadField(dbReader.rawDB, readOffset, 1);
                uint falseStringId = (uint)binaryFormattersTable.ReadField(dbReader.rawDB, readOffset, 2);
                BinaryFormatters[ID] = new BinaryDataFormatter(this, ID, falseStringId, trueStringId);
            }
        }

        void ReadModuleTypes(Table moduleTypeTable)
        {
            for (int i = 0, readOffset = (int)moduleTypeTable.offset; i < moduleTypeTable.rowCount; i++, readOffset += moduleTypeTable.rowSize)
            {
                uint id = (uint)moduleTypeTable.ReadField(dbReader.rawDB, readOffset, 0);
                uint moduleTypeId = (uint)moduleTypeTable.ReadField(dbReader.rawDB, readOffset, 3);
                uint nameId = (uint)moduleTypeTable.ReadField(dbReader.rawDB, readOffset, 1);
                ModuleTypes[moduleTypeId] = new ModuleType(this, id, moduleTypeId, nameId);
            }
        }

        void ReadStrings(Table resourceDefTable, Table stringTable1, Table stringTable2)
        {
            for (int i = 0, readOffset = (int)resourceDefTable.offset; i < resourceDefTable.rowCount; i++, readOffset += resourceDefTable.rowSize)
            {
                uint id = (uint)resourceDefTable.ReadField(dbReader.rawDB, readOffset, 0);
                uint location = (uint)resourceDefTable.ReadField(dbReader.rawDB, readOffset, 1);
                byte stringTable = (byte)(location >> 24);
                location &= 0xFFFFFF;
                string obdCode = string.Empty;
                if (!isStarScanDB)
                {
                    obdCode = Encoding.ASCII.GetString(resourceDefTable.ReadFieldRaw(dbReader.rawDB, readOffset, 3));
                }
                Table stringTableForString = (stringTable == 1 ? stringTable2 : stringTable1);
                int firstOffset = (int)(stringTableForString.offset + location);
                int lastOffset = firstOffset;
                for (; dbReader.rawDB[lastOffset] != 0; lastOffset++) ;
                string resourceString = Encoding.ASCII.GetString(dbReader.rawDB, firstOffset, lastOffset - firstOffset);
                Resources[id] = new ResourceItem(this, id, resourceString, obdCode);
            }
        }

        void ReadTXItems(Table txItems)
        {
            for (int i = 0, readOffset = (int)txItems.offset; i < txItems.rowCount; i++, readOffset += txItems.rowSize)
            {
                uint id = (uint)txItems.ReadField(dbReader.rawDB, readOffset, 0);
                byte[] conversion = txItems.ReadFieldRaw(dbReader.rawDB, readOffset, 1);
                if(!isStarScanDB)
                {
                    byte tmp = conversion[2];
                    conversion[2] = conversion[3];
                    conversion[3] = tmp;
                    tmp = conversion[4];
                    conversion[4] = conversion[5];
                    conversion[5] = tmp;
                }
                uint dataAcquireId = (uint)txItems.ReadField(dbReader.rawDB, readOffset, 2);
                uint functionId = (uint)txItems.ReadField(dbReader.rawDB, readOffset, 3);
                uint moduleMenuId = (uint)txItems.ReadField(dbReader.rawDB, readOffset, 4);
                byte[] xmit = txItems.ReadFieldRaw(dbReader.rawDB, readOffset, 6);
                uint nameId = (uint)txItems.ReadField(dbReader.rawDB, readOffset, 8);
                uint hintId = (uint)txItems.ReadField(dbReader.rawDB, readOffset, 13);
                uint moduleType = (uint)txItems.ReadField(dbReader.rawDB, readOffset, 14);
                TXItems[id] = new TXItem(this, id, conversion, dataAcquireId, moduleMenuId, functionId, xmit, nameId, hintId, moduleType);
            }
        }

        void ReadTXGroups(Table txGroupTable, Table txGroupAssociationTable)
        {
            for (int i = 0, readOffset = (int)txGroupTable.offset; i < txGroupTable.rowCount; i++, readOffset += txGroupTable.rowSize)
            {
                uint ID = (uint)txGroupTable.ReadField(dbReader.rawDB, readOffset, 0);
                uint moduleType = (uint)txGroupTable.ReadField(dbReader.rawDB, readOffset, 1);
                uint order = (uint)txGroupTable.ReadField(dbReader.rawDB, readOffset, 2);
                TXGroups[ID] = new TXItemGroup(this, ID, moduleType, order);
            }
            TXItemGroup group = null;
            for (int i = 0, readOffset = (int)txGroupTable.offset; i < txGroupTable.rowCount; i++, readOffset += txGroupTable.rowSize)
            {
                uint id = (uint)txGroupTable.ReadField(dbReader.rawDB, readOffset, 0);
                uint txid = (uint)txGroupTable.ReadField(dbReader.rawDB, readOffset, 1);
                if (TXGroups.TryGetValue(id, out group))
                {
                    group.TXItemIDs.Add(txid);
                }
            }
        }

        void ReadFunctions(Table functionTable, Table functionLinkTable)
        {
            for (int i = 0, readOffset = (int)functionTable.offset; i < functionTable.rowCount; i++, readOffset += functionTable.rowSize)
            {
                uint ID = (uint)functionTable.ReadField(dbReader.rawDB, readOffset, 0);
                uint moduleType = (uint)functionTable.ReadField(dbReader.rawDB, readOffset, 1);
                uint nameId = (uint)functionTable.ReadField(dbReader.rawDB, readOffset, 2);
                uint order = (uint)functionTable.ReadField(dbReader.rawDB, readOffset, 3);
                uint moduleMenuId = (uint)functionTable.ReadField(dbReader.rawDB, readOffset, 4);
                Functions[ID] = new Function(this, ID, moduleType, nameId, moduleMenuId, order);
            }

            for (int i = 0, readOffset = (int)functionLinkTable.offset; i < functionLinkTable.rowCount; i++, readOffset += functionLinkTable.rowSize)
            {
                uint functionIdLeft = (uint)functionLinkTable.ReadField(dbReader.rawDB, readOffset, 0);
                uint functionIdRight = (uint)functionLinkTable.ReadField(dbReader.rawDB, readOffset, 1);
                Function funcLeft = null;
                if (Functions.TryGetValue(functionIdLeft, out funcLeft))
                {
                    funcLeft.LinkedFunctionIDs.Add(functionIdRight);
                }
                if(Functions.TryGetValue(functionIdRight, out funcLeft))
                {
                    funcLeft.LinkedFunctionIDs.Add(functionIdLeft);
                }
            }
        }

        void ReadStateFormatters(Table stateFormattedDefinitions, Table stateFormatterAssociations)
        {
            for (int i = 0, readOffset = (int)stateFormatterAssociations.offset; i < stateFormatterAssociations.rowCount; i++, readOffset += stateFormatterAssociations.rowSize)
            {
                uint stringId = (uint)stateFormatterAssociations.ReadField(dbReader.rawDB, readOffset, 0);
                uint value = (uint)stateFormatterAssociations.ReadField(dbReader.rawDB, readOffset, 1);
                uint ID = (uint)stateFormatterAssociations.ReadField(dbReader.rawDB, readOffset, 3);
                StateDataFormatter stateFormatter = null;
                if (!StateFormatters.TryGetValue(ID, out stateFormatter))
                {
                    stateFormatter = new StateDataFormatter(this, ID);
                    StateFormatters[ID] = stateFormatter;
                }
                stateFormatter.StateIDs.Add(new KeyValuePair<uint, uint>(value, stringId));
            }
        }

        void ReadNumericFormatters(Table numericFormattersTable)
        {
            for (int i = 0, readOffset = (int)numericFormattersTable.offset; i < numericFormattersTable.rowCount; i++, readOffset += numericFormattersTable.rowSize)
            {
                uint ID = (uint)numericFormattersTable.ReadField(dbReader.rawDB, readOffset, 0);
                float metricConvSlope = numericFormattersTable.ReadFloatField(dbReader.rawDB, readOffset, 2);
                uint metricUnitNameId = (uint)numericFormattersTable.ReadField(dbReader.rawDB, readOffset, 3);
                float metricConvOffset = numericFormattersTable.ReadFloatField(dbReader.rawDB, readOffset, 4);
                uint englishUnitNameId = (uint)numericFormattersTable.ReadField(dbReader.rawDB, readOffset, 6);
                NumericFormatters[ID] = new NumericFormatter(this, ID, englishUnitNameId, metricUnitNameId, metricConvSlope, metricConvOffset);
            }
        }

        void ReadNumericScalers(Table numericScalerTable)
        {
            for (int i = 0, readOffset = (int)numericScalerTable.offset; i < numericScalerTable.rowCount; i++, readOffset += numericScalerTable.rowSize)
            {
                uint ID = (uint)numericScalerTable.ReadField(dbReader.rawDB, readOffset, 0);
                float slope = numericScalerTable.ReadFloatField(dbReader.rawDB, readOffset, 1);
                float offset = numericScalerTable.ReadFloatField(dbReader.rawDB, readOffset, 2);
                NumericScalers[ID] = new NumericScaler(ID, slope, offset);
            }        }

        void ReadDataAcquisitions(Table dataAcquisitionTable)
        {
            for (int i = 0, readOffset = (int)dataAcquisitionTable.offset; i < dataAcquisitionTable.rowCount; i++, readOffset += dataAcquisitionTable.rowSize)
            {
                uint ID = (uint)dataAcquisitionTable.ReadField(dbReader.rawDB, readOffset, 0);
                int requestLen = (int)dataAcquisitionTable.ReadField(dbReader.rawDB, readOffset, 1);
                int responseLen = (int)dataAcquisitionTable.ReadField(dbReader.rawDB, readOffset, 3);
                int extractOffset = (int)dataAcquisitionTable.ReadField(dbReader.rawDB, readOffset, 5);
                int extractLen = (int)dataAcquisitionTable.ReadField(dbReader.rawDB, readOffset, 6);
                uint protocol = (uint)dataAcquisitionTable.ReadField(dbReader.rawDB, readOffset, 10);
                DataAcquireMethods[ID] = new DataAcquisitionMethod(ID, protocol, requestLen, responseLen, extractOffset, extractLen);
            }
        }

        void ReadMainMenuItems(Table mainMenuTable)
        {
            for (int i = 0, readOffset = (int)mainMenuTable.offset; i < mainMenuTable.rowCount; i++, readOffset += mainMenuTable.rowSize)
            {
                uint id = (uint)mainMenuTable.ReadField(dbReader.rawDB, readOffset, 0);
                uint parentId = (uint)mainMenuTable.ReadField(dbReader.rawDB, readOffset, 1);
                uint nameId = (uint)mainMenuTable.ReadField(dbReader.rawDB, readOffset, 2);
                int order = (int)mainMenuTable.ReadField(dbReader.rawDB, readOffset, 3);
                int screenPosY = (int)mainMenuTable.ReadField(dbReader.rawDB, readOffset, 4);
                MainMenuItems[id] = new MainMenuItem(this, id, parentId, nameId, order, screenPosY);
            }
        }

        void ReadTableAttributes(SimpleBinaryReader reader, ref int readOffset, out uint tableOffset, out ushort rowCount, out ushort rowSize, out List<byte> colSizes)
        {
            tableOffset = dbReader.ReadUInt32(ref readOffset);
            rowCount = dbReader.ReadUInt16(ref readOffset);
            rowSize = dbReader.ReadUInt16(ref readOffset);

            /* While technically the 'stated' code alone was correct, it had an issue:
             * there are some columns with a size of 0! This is a waste.
             * As such, empty columns are now removed and field IDs adjusted for that. */
            byte statedColCount = dbReader.ReadUInt8(ref readOffset);
            byte[] statedColSizes = dbReader.ReadBytes(ref readOffset, statedColCount);

            /* There's actually room reserved for 27 bytes after the statedColCount,
             * so it is necessary to read past whatever bytes that go unread. */
            readOffset += 27 - statedColCount;

            colSizes = statedColSizes.Where(size => size != 0).ToList();
        }

        public string getProtocolText(ushort id)
        {
            switch (id)
            {
                case 1:
                    return "J1850";
                case 53:
                    return "CCD";
                case 60:
                    return "SCI";
                case 103:
                    return "ISO";
                case 155:
                    return "TPM";
                case 159:
                    return "Multimeter";
                case 160:
                    return "J2190 SCI";
                default:
                    return "P" + id;
            }
        }
    }
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrNull<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        {
            TValue output;
            dict.TryGetValue(key, out output);
            return output;
        }
    }
}
