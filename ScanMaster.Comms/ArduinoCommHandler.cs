using ScanMaster.Database.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScanMaster.Comms
{
    public class ArduinoCommHandler : IDisposable
    {
        StreamWriter commsLogWriter = new StreamWriter("scanmastercomms.log");
        public bool Connected { get; private set; }
        public List<byte> ResponseBuffer { get; private set; }
        SerialPort arduino;
        Task readTask;
        ManualResetEvent responseReceived = new ManualResetEvent(false);
        Protocol currentMessageProtocol = Protocol.Invalid;
        private ArduinoCommHandler(SerialPort arduino)
        {
            ResponseBuffer = new List<byte>();
            this.arduino = arduino;
            readTask = new Task(ReadThread);
            readTask.Start();
            Connected = false;
        }

        public bool EstablishComms()
        {
            Connected = true;
            //if(!Connected)
            //{
            //    responseReceived.Reset();
            //    responseReceived.WaitOne(2000);
            //}
            return Connected;
        }

        public void Dispose()
        {
            arduino.Close();
            arduino.Dispose();
            readTask.Wait();
            readTask.Dispose();
            commsLogWriter.Dispose();
        }

        public List<byte> SendMessageAndGetResponse(Protocol protocol, params byte[] message)
        {
            currentMessageProtocol = protocol;
            List<byte> bytesForXmega = new List<byte>(message);
            bytesForXmega.Insert(0, (byte)(0x80 | (message.Length + 1)));
            bytesForXmega.Insert(1, (byte)(protocol));
            responseReceived.Reset();
            ResponseBuffer.Clear();
            for (int retry = 0; retry < 3; retry++)
            {
                arduino.Write(bytesForXmega.ToArray(), 0, bytesForXmega.Count);
                string strLogMsg = "(TX) ";
                foreach(byte by in bytesForXmega)
                {
                    strLogMsg += string.Format("{0:X2} ", by);
                }
                commsLogWriter.WriteLine(strLogMsg.TrimEnd(' '));
                if (responseReceived.WaitOne(500))
                {
                    return new List<byte>(ResponseBuffer);
                }
            }
            return new List<byte>();
        }

        public bool GetSCIByte(ref byte receivedByte)
        {
            responseReceived.Reset();
            if (!responseReceived.WaitOne(150)) return false;
            if (ResponseBuffer.Count != 1) throw new ArgumentException(); // sum ting wong
            receivedByte = ResponseBuffer[0];
            return true;
        }

        public bool GetSCIBytes(int count, out List<byte> sciBytes)
        {
            sciBytes = new List<byte>(count);
            byte recdByteTemp = 0;
            for (int i = 0; i < count; i++)
            {
                if (!GetSCIByte(ref recdByteTemp)) return false;
                sciBytes.Add(recdByteTemp);
            }
            return true;
        }

        void ProcessMessage(List<byte> messageHex)
        {
            if (messageHex.Count >= 1)
            {
                string logMsg = "(RX) ";
                foreach (byte by in messageHex)
                {
                    logMsg += string.Format("{0:X2} ", by);
                }
                commsLogWriter.WriteLine(logMsg);
                if ((messageHex[0] & 0x80) == 0 || messageHex.Count < 3) return;
                if (((Protocol)messageHex[1]) != currentMessageProtocol) return;
                List<byte> messageHexTrim = new List<byte>(messageHex.Skip(2));
                bool isResponse = false;
                if (currentMessageProtocol == Protocol.CCD || currentMessageProtocol == Protocol.CCD_2)
                {
                    isResponse = (messageHexTrim[0] == 0xF2 && messageHexTrim.Count == 5);
                }
                else if (currentMessageProtocol == Protocol.J1850)
                {
                    isResponse = (messageHexTrim.Count > 2 && messageHexTrim[0] == 0x26 && ((messageHexTrim[2] & 0x40) != 0));
                }
                else //if (connectedProtocol == Protocol.SCI || connectedProtocol == Protocol.SCI_NGC || connectedProtocol == Protocol.KWP2000)
                {
                    isResponse = true;
                }
                if (isResponse)
                {
                    ResponseBuffer = messageHexTrim;
                    responseReceived.Set();
                }
            }
        }

        public void SendCommand(CommandID cmd)
        {
            arduino.Write(new byte[] { (byte)cmd }, 0, 1);
            responseReceived.Reset();
            responseReceived.WaitOne(100);
            commsLogWriter.WriteLine(string.Format("(TX) {0:X2}", cmd));
        }

        public static ArduinoCommHandler CreateCommHandler(string portName)
        {
            SerialPort serial = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
            serial.DtrEnable = true;
            serial.RtsEnable = true;
            try
            {
                serial.Open();
            }
            catch(IOException)
            {
                return null;
            }
            return new ArduinoCommHandler(serial);
        }
        
        void ReadThread()
        {
            try
            {
                List<byte> bytes = new List<byte>();
                while (true)
                {
                    byte temp = 0;
                    try
                    {
                        temp = (byte)arduino.ReadByte();
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }
                    bytes.Add(temp);
                    if((bytes[0] & 0x80) == 0 || (bytes[0] & 0x7F) == (bytes.Count - 1))
                    {
                        ProcessMessage(bytes);
                        bytes.Clear();
                    }
                    Thread.Sleep(1);
                }
            }
            catch (IOException) { }
            catch (InvalidOperationException) { }
        }
    }
}
