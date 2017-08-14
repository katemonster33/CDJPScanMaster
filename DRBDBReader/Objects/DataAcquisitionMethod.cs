using ScanMaster.Database.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanMaster.Database.Objects
{
    public class DataAcquisitionMethod
    {
        public uint ID { get; private set; }
        public Protocol Protocol { get; private set; }
        public int RequestLen { get; private set; }
        public int ResponseLen { get; private set; }
        public int ExtractOffset { get; private set; }
        public int ExtractLen { get; private set; }
        public DataAcquisitionMethod(uint id, uint protocol, int requestLen, int responseLen, int extractOffset, int extractLen)
        {
            ID = id;
            Protocol = (Protocol)protocol;
            RequestLen = requestLen;
            ResponseLen = responseLen;
            ExtractOffset = extractOffset;
            ExtractLen = extractLen;
        }

        public byte[] GetRequest(byte[] requestRaw)
        {
            byte[] output = new byte[RequestLen];
            Buffer.BlockCopy(requestRaw, 0, output, 0, RequestLen);
            return output;
        }

        public byte[] ExtractData(byte[] response)
        {
            byte[] output = new byte[ExtractLen];
            Buffer.BlockCopy(response, ExtractOffset, output, 0, ExtractLen);
            return output;
        }
    }
}
