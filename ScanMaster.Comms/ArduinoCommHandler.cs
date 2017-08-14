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
        public Protocol ConnectedProtocol { get; private set; }
        public List<byte> ResponseBuffer { get; private set; }
        SerialPort arduino;
        Task readTask;
        ManualResetEvent responseReceived = new ManualResetEvent(false);
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
            if(!Connected)
            {
                responseReceived.Reset();
                responseReceived.WaitOne(2000);
            }
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

        byte readHex(char hexA, char hexB)
        {
            return (byte)((readHex(hexA) << 4) | readHex(hexB));
        }

        byte readHex(char hex)
        {
            if (hex >= '0' && hex <= '9') return (byte)(hex - '0');
            else if (hex >= 'a' && hex <= 'f') return (byte)(hex - 'a' + 10);
            else if (hex >= 'A' && hex <= 'F') return (byte)(hex - 'A' + 10);
            else throw new ArgumentException();
        }

        bool isHexString(string str)
        {
            foreach (char c in str)
            {
                if (!(char.IsDigit(c) || (c >= 'A' && c <= 'F'))) return false;
            }
            return true;
        }

        public List<byte> SendMessageAndGetResponse(params byte[] message)
        {
            string messageAscii = string.Empty;
            for (int i = 0; i < message.Length; i++)
            {
                messageAscii += string.Format("{0:X2}", message[i]);
            }
            responseReceived.Reset();
            ResponseBuffer.Clear();
            for (int retry = 0; retry < 3; retry++)
            {
                arduino.Write(messageAscii + '\n');
                commsLogWriter.WriteLine("(TX) " + messageAscii);
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

        void ProcessMessage(string message)
        {
            if (message == "OKGO")
            {
                Connected = true;
                return;
            }
            else if (!isHexString(message))
            {
                responseReceived.Set();
                return;
            }
            List<byte> messageHex = new List<byte>();
            for (int i = 0; i < message.Length; i += 2)
            {
                messageHex.Add(readHex(message[i], message[i + 1]));
            }
            if (messageHex.Count >= 1)
            {
                bool isResponse = false;
                if (ConnectedProtocol == Protocol.CCD || ConnectedProtocol == Protocol.CCD_2)
                {
                    isResponse = (messageHex[0] == 0xF2 && messageHex.Count == 5);
                }
                else if (ConnectedProtocol == Protocol.J1850)
                {
                    isResponse = (messageHex.Count > 2 && messageHex[0] == 0x26 && ((messageHex[2] & 0x40) != 0));
                }
                else //if (connectedProtocol == Protocol.SCI || connectedProtocol == Protocol.SCI_NGC || connectedProtocol == Protocol.KWP2000)
                {
                    isResponse = true;
                }
                if (isResponse)
                {
                    commsLogWriter.WriteLine("(RX) " + message);
                    ResponseBuffer = messageHex;
                    responseReceived.Set();
                }
            }
        }

        public void ConnectChannel(ArduinoCommChannel channel)
        {
            string channelCommand = string.Format("{0:d}\n", (int)channel);
            arduino.Write(channelCommand);
            responseReceived.Reset();
            commsLogWriter.WriteLine("(TX) " + channelCommand.TrimEnd('\n'));
            responseReceived.WaitOne(1000);
            switch(channel)
            {
                case ArduinoCommChannel.CCD:
                    ConnectedProtocol = Protocol.CCD;
                    break;
                case ArduinoCommChannel.J1850:
                    ConnectedProtocol = Protocol.J1850;
                    break;
                case ArduinoCommChannel.SCI_A_Engine:
                case ArduinoCommChannel.SCI_B_Engine:
                    ConnectedProtocol = Protocol.SCI;
                    break;
                case ArduinoCommChannel.ISO9141:
                    ConnectedProtocol = Protocol.KWP2000;
                    break;
            }
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
                string buf = string.Empty;
                while (true)
                {
                    try
                    {
                        buf = arduino.ReadLine();
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }
                    ProcessMessage(buf.TrimEnd('\r'));
                    Thread.Sleep(1);
                }
            }
            catch (IOException) { }
            catch (InvalidOperationException) { }
        }
    }
}
