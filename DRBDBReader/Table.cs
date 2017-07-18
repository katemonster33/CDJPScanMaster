using System;
using System.Collections.Generic;

namespace DRBDB
{
    public class Table
    {
        public uint offset;
        private ushort id;
        public ushort rowCount;
        public ushort rowSize;
        List<byte> colSizes;
        List<ushort> colOffsets = new List<ushort>();
        public Database db;

        public Table(Database db, ushort id, uint offset, ushort rowCount, ushort rowSize, List<byte> colSizesLst)
        {
            this.db = db;
            this.id = id;
            this.offset = offset;
            this.rowCount = rowCount;
            this.rowSize = rowSize;
            colSizes = colSizesLst;
            ushort colOffset = 0;
            foreach(byte cnt in colSizes)
            {
                colOffsets.Add(colOffset);
                colOffset += cnt;
            }
        }

        // This saves us a ridiculous amount of memory.
        // GC catches it otherwise of course, but without this
        // we were making 1GB of allocations. Now it's not even 20MB.
        private byte[] scratch = new byte[8];
        public ulong ReadField(byte[] database, int readOffset, byte colIndex)
        {
            ushort colSize = colSizes[colIndex];
            ushort colOffset = colOffsets[colIndex];
            if (colSize == 1)
            {
                return database[readOffset + colOffset];
            }
            if (!this.db.isStarScanDB)
            {
                /* While Array.Copy should be faster due to presumably being a native memcpy,
				 * it turns out that the combo of needing Array.Reverse as well is awful.
				 * So although the following code is slower than a memcpy, the fact that it
				 * avoids Array.Reverse makes it much much faster, especially for smaller fields.
				 */
                int idx = readOffset + colOffset + colSize - 1;
                for (int i = 0; i < colSize; ++i)
                {
                    this.scratch[i] = database[idx - i];
                }
                switch (colSize)
                {
                    case 2:
                        return BitConverter.ToUInt16(this.scratch, 0);
                    case 4:
                        return BitConverter.ToUInt32(this.scratch, 0);
                    case 8:
                        return BitConverter.ToUInt64(this.scratch, 0);
                    default:
                        throw new ArgumentException();
                }
            }
            else
            {
                /* StarSCAN's database already has endianness taken care of,
				 * so it's OK to just do a direct copy of the data.
				 */
                switch (colSize)
                {
                    case 2:
                        return BitConverter.ToUInt16(database, readOffset + colOffset);
                    case 4:
                        return BitConverter.ToUInt32(database, readOffset + colOffset);
                    case 8:
                        return BitConverter.ToUInt64(database, readOffset + colOffset);
                    default:
                        throw new ArgumentException();
                }
            }
        }

        public float ReadFloatField(byte[] database, int readOffset, byte colIndex)
        {
            return BitConverter.ToSingle(database, readOffset + colOffsets[colIndex]);
        }

        public byte[] ReadFieldRaw(byte[] table, int readOffset, byte col)
        {
            byte[] ret = new byte[colSizes[col]];
            Array.Copy(table, readOffset + colOffsets[col], ret, 0, ret.Length);
            return ret;
        }
    }
}
