using DRBDB;
using DRBDB.Enums;
using DRBDB.Objects;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.IO;

namespace CDJPScanMaster
{
    public partial class Form1 : Form
    {
        SerialPort arduino;
        bool connected = false;
        Task readTask, queryTask;
        ManualResetEvent queryTaskStopSignal = new ManualResetEvent(false);
        Protocol connectedProtocol = Protocol.CCD;
        Module selectedModule = null;
        ManualResetEvent responseReceived = new ManualResetEvent(false);
        List<byte> response = new List<byte>();
        Database drbdb = new Database();
        public Form1()
        {
            InitializeComponent();
            foreach(ModuleType type in drbdb.GetModuleTypes())
            {
                listBox1.Items.Add(type);
            }
            listBox1.Tag = drbdb.GetModuleTypes();
            arduino = new SerialPort("COM16", 115200, Parity.None, 8, StopBits.One);
            arduino.DtrEnable = true;
            arduino.RtsEnable = true;
            arduino.Open();
            readTask = new Task(ReadThread);
            readTask.Start();
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
        List<TXItem> visibleTxItems = new List<TXItem>();
        void QueryThread()
        {
            queryTaskStopSignal.Reset();
            bool isHighSpeedSciMode = false;
            while (!queryTaskStopSignal.WaitOne(1))
            {
                for (int i = 0; i < visibleTxItems.Count; i++)
                {
                    TXItem tx = visibleTxItems[i];
                    if (tx.TransmitBytes.Length == 0 || tx.DataAcquisitionMethod == null || (tx.TransmitBytes.Length == 1 && tx.TransmitBytes[0] == 0)) continue;
                    if (connectedProtocol == Protocol.SCI)
                    {
                        if (!isHighSpeedSciMode && tx.TransmitBytes[0] >= 0xF0)
                        {
                            SendMessageAndGetResponse(0x12);
                            isHighSpeedSciMode = true;
                        }
                        if (isHighSpeedSciMode && tx.TransmitBytes[0] < 0xF0)
                        {
                            SendMessageAndGetResponse(0xFE);
                            Thread.Sleep(165);
                            isHighSpeedSciMode = false;
                        }
                    }
                    byte[] xmitTemp = new byte[tx.DataAcquisitionMethod.RequestLen];
                    Array.Copy(tx.TransmitBytes, xmitTemp, tx.DataAcquisitionMethod.RequestLen);
                    List<byte> response = SendMessageAndGetResponse(xmitTemp);
                    if (response.Count == tx.DataAcquisitionMethod.ResponseLen)
                    {
                        byte[] dataBytes = tx.DataAcquisitionMethod.ExtractData(response.ToArray());
                        tx.DataDisplay.RawData = dataBytes;
                        int tmpIndex = i;
                        if (tx.DataDisplay.IsRawDataUpdated)
                        {
                            string data = tx.DataDisplay.FormattedData;
                            lstDataMenuTXs.BeginInvoke((Action)(() => lstDataMenuTXs.Items[tmpIndex].SubItems[1].Text = data));
                        }
                    }
                }
            }
        }

        void ConnectChannel(ArduinoCommChannel channel)
        {
            string channelCommand = string.Format("{0:d}\n", (int)channel);
            arduino.Write(channelCommand);
            responseReceived.Reset();
            txtSerialLog.InvokeIfRequired(() => txtSerialLog.AppendText("(TX) " + channelCommand));
            responseReceived.WaitOne(1000);
        }

        void ProcessMessage(string message)
        {
            txtSerialLog.BeginInvoke((Action)(() => txtSerialLog.AppendText("(RX) " + message + '\n')));
            if (message == "OKGO")
            {
                connected = true;
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
                if (connectedProtocol == Protocol.CCD || connectedProtocol == Protocol.CCD_2)
                {
                    isResponse = (messageHex[0] == 0xF2 && messageHex.Count == 5);
                }
                else if (connectedProtocol == Protocol.J1850)
                {
                    isResponse = (messageHex.Count > 2 && messageHex[0] == 0x26 && ((messageHex[2] & 0x40) != 0));
                }
                else //if (connectedProtocol == Protocol.SCI || connectedProtocol == Protocol.SCI_NGC || connectedProtocol == Protocol.KWP2000)
                {
                    isResponse = true;
                }
                if(isResponse)
                {
                    response = messageHex;
                    responseReceived.Set();
                }
            }
        }

        bool isHexString(string str)
        {
            foreach(char c in str)
            {
                if (!(char.IsDigit(c) || (c >= 'A' && c <= 'F'))) return false;
            }
            return true;
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

        void ConnectModuleType(ModuleType moduleToConnect)
        {
            string year = string.Empty, bodyStyle = string.Empty;
            switch ((ModuleTypeID)moduleToConnect.TypeID)
            {
                case ModuleTypeID.Engine:
                    string engineSize = string.Empty, ecmType = string.Empty;
                    ConnectChannel(ArduinoCommChannel.SCI_A_Engine);
                    if (!GetEngineConfigSCI(ref engineSize, ref year, ref bodyStyle, ref ecmType))
                    {
                        ConnectChannel(ArduinoCommChannel.SCI_B_Engine);
                        if (!GetEngineConfigSCI(ref engineSize, ref year, ref bodyStyle, ref ecmType))
                        {
                            MessageBox.Show("Failed to identify engine over SCI, A configuration or B configuration.");
                            return;
                        }
                    }
                    uint engineModuleId = Get_Engine_Table_ID(engineSize, year, bodyStyle, ecmType);
                    if(engineModuleId == 0)
                    {
                        MessageBox.Show("Engine appears to be unsupported.\n" + 
                            "Engine size: " + engineSize + "\n" + 
                            "Year: " + year + "\n" + 
                            "Body Style: " + bodyStyle + "\n" + 
                            "ECM Type: " + ecmType);
                        return;
                    }
                    LoadModuleID(engineModuleId);
                    break;

                case ModuleTypeID.Transmission:
                    break;

                case ModuleTypeID.Body:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return;
                    }
                    string level = GetBCMModuleLevel_CCD();
                    if (level == null)
                    {
                        level = "Base"; //it's possible the BCM doesn't support this command ID - in that case, it's likely just a base module.
                    }
                    uint moduleId = Get_Body_Table_ID(year, bodyStyle, level);
                    if (moduleId == 0)
                    {
                        MessageBox.Show("This combination of BCM appears to be unsupported. \n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle + "\n" +
                            "BCM Style: " + level);
                        return;
                    }
                    LoadModuleID(moduleId);
                    break;

                case ModuleTypeID.Brakes:
                    break;

                case ModuleTypeID.AudioSystems:
                    string radioModel = GetRadioModel();
                    if(string.IsNullOrEmpty(radioModel))
                    {
                        MessageBox.Show("Failed to identify audio module. Note that this is normal if a factory radio is installed.");
                    }
                    uint radioTableId = Get_Radio_Table_ID(radioModel);
                    if(radioTableId == 0)
                    {
                        MessageBox.Show("Identified radio was not supported: " + radioModel);
                        return;
                    }
                    LoadModuleID(radioTableId);
                    break;

                case ModuleTypeID.VehicleTheftSecurity:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return;
                    }
                    bool isImmob = true; // USE THIS FOR DETERMINING COMM LATER
                    uint vtssTableId = Get_VTSS_Table_ID(year, bodyStyle, isImmob);
                    if(vtssTableId == 0)
                    {
                        MessageBox.Show("This theft module appears to be unsupported. \n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle + "\n" +
                            "Is Immobilizer: " + isImmob.ToString());
                        return;
                    }
                    LoadModuleID(vtssTableId);
                    break;

                case ModuleTypeID.AirTempControl:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return;
                    }
                    uint airTempTableId = Get_AirTempControl_Table_ID(bodyStyle);
                    if (airTempTableId == 0)
                    {
                        MessageBox.Show("This air temp control module appears to be unsupported. \n" +
                            "Body Style: " + bodyStyle);
                    }
                    LoadModuleID(airTempTableId);
                    break;

                case ModuleTypeID.CompassMiniTrip:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return;
                    }
                    uint compassMiniTripId = Get_CompassMiniTrip_Table_ID(bodyStyle);
                    if(compassMiniTripId == 0)
                    {
                        MessageBox.Show("This compass mini-trip module appears to be unsupported. \n" +
                            "Body Style: " + bodyStyle);
                    }
                    LoadModuleID(compassMiniTripId);
                    break;

                case ModuleTypeID.MemorySeat:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return;
                    }
                    uint memorySeatId = Get_MemorySeat_Table_ID(bodyStyle, year);
                    if(memorySeatId == 0)
                    {
                        MessageBox.Show("This compass mini-trip module appears to be unsupported. \n" +
                            "Year: " + year + "\n" + 
                            "Body Style: " + bodyStyle);
                        return;
                    }
                    LoadModuleID(memorySeatId);
                    break;

                case ModuleTypeID.InstrumentCluster:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return;
                    }
                    uint clusterId = Get_MIC_Table_ID(bodyStyle, year);
                    if(clusterId == 0)
                    {
                        MessageBox.Show("This instrument cluster module appears to be unsupported. \n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle);
                        return;
                    }
                    LoadModuleID(clusterId);
                    break;

                case ModuleTypeID.VehicleInfoCenter:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return;
                    }
                    uint vicId = Get_VehicleInfoCenter_Table_ID(bodyStyle, year);
                    if(vicId == 0)
                    {
                        MessageBox.Show("This vehicle info center module appears to be unsupported. \n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle);
                        return;
                    }
                    LoadModuleID(vicId);
                    break;

                case ModuleTypeID.Otis:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return;
                    }
                    uint otisId = Get_OTIS_Table_ID(bodyStyle);
                    if(otisId == 0)
                    {
                        MessageBox.Show("This OTIS module appears to be unsupported. \n" +
                            "Body Style: " + bodyStyle);
                        return;
                    }
                    LoadModuleID(otisId);
                    break;
            }
        }

        uint Get_Engine_Table_ID(string engineSize, string year, string bodyStyle, string ecmType)
        {
            Module mostQualifiedModule = null;
            uint maxQualifiers = 0;
            foreach(Module mod in drbdb.GetModules().Where(mod => mod.ModuleTypeID == (uint)ModuleTypeID.Engine))
            {
                uint tempQualifiers = mod.TestModuleQualifiers(engineSize, year, bodyStyle, ecmType);
                if(tempQualifiers > maxQualifiers)
                {
                    maxQualifiers = tempQualifiers;
                    mostQualifiedModule = mod;
                }
            }
            if (mostQualifiedModule == null) return 0;
            return mostQualifiedModule.ID;
        }

        bool GetEngineConfigSCI(ref string engineSize, ref string year, ref string bodyStyle, ref string ecuType)
        {
            connectedProtocol = Protocol.SCI;
            // try SBECII
            List<byte> sciResp = SendMessageAndGetResponse(0x16, 0x81);
            if (sciResp.Count == 2 && GetSCIBytes(5, out sciResp))
            {
                year = (1990 + (sciResp[2] & 0x0F)).ToString();
                engineSize = GetEngineSize_SBECII((byte)(sciResp[2] >> 4), (byte)((sciResp[3] >> 3) & 7));
                ecuType = "SBEC/SBECII";
                sciResp = SendMessageAndGetResponse(0x16, 0x82);
                if (sciResp.Count != 2 || !GetSCIBytes(5, out sciResp)) return false;//not sure how to handle this, failure is the only way
                bodyStyle = GetBodyStyle_SBECII(sciResp[0], sciResp[1], sciResp[2]);
                return true;
            }
            else // try SBECIII / JTEC
            {
                byte mode2AData = 0;
                sciResp = SendMessageAndGetResponse(0x2A, 0x0B);
                if (sciResp.Count != 2 || !GetSCIByte(ref mode2AData)) return false;

                year = (1990 + mode2AData).ToString();
                sciResp = SendMessageAndGetResponse(0x2A, 0x0C);
                if (sciResp.Count != 2 || !GetSCIByte(ref mode2AData)) return false;
                engineSize = GetEngineSize_SBECIII_JTEC(mode2AData);
                sciResp = SendMessageAndGetResponse(0x2A, 0x0F);
                if (sciResp.Count != 2 || !GetSCIByte(ref mode2AData)) return false;
                ecuType = GetEngineControllerType_Mode2A(mode2AData);
                sciResp = SendMessageAndGetResponse(0x2A, 0x10);
                if (sciResp.Count != 2 || !GetSCIByte(ref mode2AData)) return false;
                bodyStyle = GetBodyStyle_SBECIII_JTEC(mode2AData);
                return true;
            }
        }

        string GetBodyStyle_SBECIII_JTEC(byte style)
        {
            switch(style)
            {
                case 1:
                    return "YJ";
                case 2:
                    return "XJ";
                case 3:
                    return "ZJ";
                case 4:
                    return "FJ";
                case 5:
                    return "PL";
                case 6:
                    return "JA";
                case 7:
                    return "AA";
                case 8:
                    return "AC";
                case 9:
                    return "AS";
                case 10:
                    return "LH";
                case 11:
                    return "NS";
                case 12:
                    return "AB";
                case 13:
                    return "AN";
                case 14:
                    return "BR";
                case 15:
                    return "SR";
                case 16:
                    return "AN";
                case 17:
                    return "AN";
                case 18:
                    return "JX";
                case 19:
                    return "PR";
                case 20:
                    return "TJ";
                case 21:
                    return "DN";
                case 22:
                    return "WJ";
                case 23:
                    return "SJ";
                case 24:
                    return "JR";
                case 25:
                    return "PT";
                case 26:
                    return "RS";
                case 27:
                    return "KJ";
                case 28:
                    return "DR";
                case 29:
                    return "AN84";
                case 30:
                    return "ZB";
            }
            throw new ArgumentException("(BARF)");
        }

        string GetBodyStyle_SBECII(byte b1, byte b2, byte b3)
        {
            switch (b1 & 0x07)
            {
                case 1:
                    return "YJ";
                case 2:
                    return "XJ";
                case 4:
                    return "ZJ";
            }
            switch (b2)
            {
                case 2:
                    return "FJ";
                case 4:
                    return "PL";
                case 8:
                    return "JA";
                case 16:
                    return "AA"; //  AA/AG/AJ/AP 
                case 32:
                    return "AC"; //  AC/AY
                case 64:
                    return "AS";
                case 128:
                    return "LH";
            }
            switch (b3)
            {
                case 1:
                    return "AB";
                case 2:
                    return "AN";
                case 4:
                    return "BR";
                case 8:
                    return "SR";
            }
            throw new ArgumentException("(BAAAARF)");
        }

        string GetEngineSize_SBECIII_JTEC(byte engineCfg)
        {
            switch(engineCfg)
            {
                case 1:
                    return "2.2L I4";
                case 2:
                    return "2.5L I4";
                case 3:
                    return "3.0L V6";
                case 4:
                    return "3.3L V6";
                case 5:
                    return "3.9L V6";
                case 6:
                    return "5.2L V8";
                case 7:
                    return "5.9L V8";
                case 8:
                    return "3.8L V6";
                case 9:
                    return "4.0L I6";
                case 10:
                    return "2.0L I4 SOHC";
                case 11:
                    return "3.5L V6";
                case 12:
                    return "8.0L V10";
                case 13:
                    return "2.4L I4";
                case 14:
                    return "2.5L I4";
                case 15:
                    return "2.5L V6";
                case 16:
                    return "2.0L I4 DOHC";
                case 17:
                    return "2.5L V6";
                case 18:
                    return "5.9L I6";
                case 19:
                    return "3.3L V6";
                case 20:
                    return "2.7L V6";
                case 21:
                    return "3.2L V6";
                case 22:
                    return "1.8L I4";
                case 23:
                    return "3.7L V6";
                case 24:
                    return "4.7L V8";
                case 25:
                    return "1.9L I4";
                case 26:
                    return "3.1L I5";
                case 27:
                    return "1.6L I4";
                case 28:
                    return "2.7L V6";
                case 29:
                    return "5.7L V8";
                case 30:
                    return "8.3L V10";
                case 31:
                    return "2.7L I5";
                case 32:
                    return "2.8L I4";
            }
            throw new ArgumentException("(BARF)");
        }

        string GetEngineControllerType_Mode2A(byte ecmType)
        {
            switch(ecmType)
            {
                case 1:
                    return "FCC";
                case 2:
                case 3:
                case 4:
                    return "SBEC/SBECII";
                case 5:
                    return "SBECIII";
                case 6:
                case 13:
                case 20:
                    return "JTEC";
                case 7:
                    return "SBECIIIA";
                case 8:
                    return "SBECIII+";
                case 9:
                    return "CM551";
                case 10:
                case 15:
                case 16:
                case 25:
                    return "Bosch";
                case 11:
                    return "Northrop";
                case 12:
                case 14:
                    return "JTEC+";
                case 17:
                    return "Siemens";
                case 18:
                    return "SBECIIIA+";
                case 19:
                    return "SBECIIIB";
                case 21:
                    return "CM845";
                case 22:
                    return "CM846";
                case 23:
                    return "Cummins";
                case 24:
                    return "CM848";
            }
            throw new ArgumentException("(BARF)");
        }

        string GetEngineSize_SBECII(byte engineCfg, byte manufacturer)
        {
            switch(engineCfg)
            {
                case 0:
                    return "2.2L I4";
                case 1:
                    return "2.5L I4";
                case 2:
                    return "3.0L V6";
                case 3:
                    return "3.3L V6";
                case 4:
                    return "3.9L V6";
                case 5:
                    return "5.2L V8";
                case 6:
                    if(manufacturer == 6) //Cummins
                    {
                        return "5.9L I6";
                    }
                    else
                    {
                        return "5.9L V8";
                    }
                case 7:
                    return "3.8L V8";
                case 8:
                    return "4.0L I6";
                case 9:
                    return "2.0L I4 SOHC";
                case 10:
                    return "3.5L V6";
                case 11:
                    return "8.0L V10";
                case 12:
                    return "2.4L I4";
                case 13:
                    return "2.0L I4 DOHC";
            }
            throw new ArgumentException("i dont know what to do with this");
        }

        bool GetSCIByte(ref byte receivedByte)
        {
            responseReceived.Reset();
            if (!responseReceived.WaitOne(150)) return false;
            if (response.Count != 1) throw new ArgumentException(); // sum ting wong
            receivedByte = response[0];
            return true;
        }

        bool GetSCIBytes(int count, out List<byte> sciBytes)
        {
            sciBytes = new List<byte>(count);
            byte recdByteTemp = 0;
            for(int i=0; i < count; i++)
            {
                if (!GetSCIByte(ref recdByteTemp)) return false;
                sciBytes.Add(recdByteTemp);
            }
            return true;
        }

        uint Get_SentryKeyModule_Table_ID(string year)
        {
            if (connectedProtocol == Protocol.CCD) return 4181;
            else
            {
                if (int.Parse(year) >= 2003) return 4327; //SKREEM
                else return 4182; //SKIM PCI
            }
        }

        uint Get_VTSS_Table_ID(string year, string bodyStyle, bool isImmob)
        {
            switch(bodyStyle)
            {
                case "JA":
                case "JX":
                    if (isImmob) return 4128;
                    else return 4126;
                case "ZJ":
                    return 4127;
                case "TJ":
                    return 4128; 
                case "ZG":
                    return 4129;
                case "VIPER":
                    return 4130;
                case "AN":
                case "AB":
                case "DN":
                case "BR":
                case "BE":
                    return 4131;
                case "XJ":
                    if (isImmob) return 4132;
                    else return 4136;
                case "PROWLER":
                    if (year.StartsWith("200")) return 4134; //200X
                    else return 4133;
                case "LH":
                    return 4135;
                case "NS":
                    return 4137;
                case "GS":
                    return 4138;
                case "WJ":
                    return 4139;
                case "PL":
                case "PT":
                    return 4140;
                case "ITM": // ???
                    return 4273;
                case "THATC": // ???
                    return 4306;
            }
            return 0;
        }

        uint Get_AirTempControl_Table_ID(string bodyStyle)
        {
            switch(bodyStyle)
            {
                case "ZJ":
                    return 4155;
                case "LH":
                    return 4156;
                case "WJ":
                    return 4157;
                case "RS":
                    return 4158;
                case "JR":
                    return 4288;
                case "CS":
                    return 4329;
            }
            return 0;
        }

        uint Get_MIC_Table_ID(string bodyStyle, string year)
        {
            switch(bodyStyle)
            {
                case "JA":
                case "JX":
                    // IF METRIC??
                    return 1495;
                case "NS":
                case "GS":
                    return 1496;
                case "ZJ":
                    return 1497;
                case "TJ":
                    if (connectedProtocol == Protocol.J1850) return 4212;
                    else return 1498;
                case "AN":
                    if (connectedProtocol == Protocol.J1850) return 4014;
                    else return 1499;
                case "XJ":
                    return 1500;
                case "PR":
                    return 1501;
                case "WJ":
                    if (int.Parse(year) >= 2002) return 4301;
                    else return 4007;
                case "LH":
                    // CONCORDE/INTREPID vs LHS/300M ???
                    return 4008;
                case "DN":
                    if (connectedProtocol == Protocol.J1850) return 4014;
                    else return 4009;
                case "AB":
                    return 4010;
                case "BR":
                    return 4011;
                case "BE":
                    return 4012;
                case "PL":
                    if (int.Parse(year) >= 2002) return 4298;
                    else return 4013;
                case "JR":
                    return 4016;
                case "RS":
                case "RG":
                    return 4017;
                case "PT":
                    if (int.Parse(year) >= 2002) return 4298;
                    else return 4018;
                case "KJ":
                    return 4259;
                case "DR":
                    return 4271;
                case "WG":
                    return 4301;
                case "ZB":
                    return 4314;
                case "CS":
                    return 4331;
            }
            return 0;
        }

        uint Get_OTIS_Table_ID(string bodyStyle)
        {
            if (bodyStyle == "LH") return 4055;
            else return 0;
        }

        uint Get_CompassMiniTrip_Table_ID(string bodyStyle)
        {
            switch(bodyStyle)
            {
                case "ZJ":
                    return 4047;
                case "PR":
                    return 4048;
                case "NS":
                case "GS":
                    return 4049;
                case "AN":
                case "DN":
                    if (connectedProtocol == Protocol.J1850) return 4054;
                    else return 4050;
                case "XJ":
                    return 4051;
                case "TJ":
                    return 4052;
                case "BR":
                case "BE":
                    return 4052;
                case "DR":
                case "KJ":
                case "LH":
                    return 4285;
                case "RS":
                case "RG":
                    return 4308;
            }
            return 0;
        }

        string GetRadioModel()
        {
            connectedProtocol = Protocol.CCD;
            ConnectChannel(ArduinoCommChannel.CCD);
            List<byte> ccdResponse = SendMessageAndGetResponse(0xB2, 0x96, 0x24, 0x10, 0x00);
            if(ccdResponse.Count == 5)
            {
                string model = string.Empty;
                model += (char)ccdResponse[3];

                ccdResponse = SendMessageAndGetResponse(0xB2, 0x96, 0x24, 0x11, 0x00);
                if (ccdResponse.Count != 5) return string.Empty;
                model += (char)ccdResponse[3];

                ccdResponse = SendMessageAndGetResponse(0xB2, 0x96, 0x24, 0x12, 0x00);
                if (ccdResponse.Count != 5) return string.Empty;
                model += (char)ccdResponse[3];
                return model;
            }
            else
            {
                connectedProtocol = Protocol.J1850;
                ConnectChannel(ArduinoCommChannel.J1850);
                List<byte> j1850Response = SendMessageAndGetResponse(0x24, 0x80, 0x22, 0x20, 0x01, 0x00);
                if (j1850Response.Count != 6) return string.Empty;
                return Encoding.ASCII.GetString(j1850Response.ToArray(), 3, 3);
            }
        }

        uint Get_Radio_Table_ID(string radioModel)
        {
            switch(radioModel)
            {
                case "RBC":
                case "RBR":
                    return 4032;
                case "RAD":
                    return 4033;
                case "RAZ":
                    if (connectedProtocol == Protocol.J1850) return 4035;
                    else return 4034;
                case "RBN":
                    if (connectedProtocol == Protocol.J1850) return 4037;
                    else return 4036;
                case "RBL":
                    return 4038;
                case "RBJ":
                    return 4039;
                //case "RBR":
                //    return 4038;
                case "RBA":
                    return 4041;
                case "RBT":
                    return 4042;
                case "RBY":
                    return 4043;
                case "RBB":
                    return 4044;
                case "RBP":
                    return 4045;
                case "RAS":
                    return 4046;
                case "RBU":
                    return 4292;
                case "RBK":
                    return 4293;
                case "RB1":
                    return 4343;
                case "RB3":
                    return 4351;
                case "REV":
                    return 4363;
                case "RAH":
                    return 4364;
                case "RBQ": // viper, probably don't matter
                    return 4368;

            }
            return 0;
        }

        uint Get_TirePressureMonitor_Table_ID(string bodyStyle)
        {
            if (bodyStyle == "PROWLER") return 4173;
            else return 0;
        }

        uint Get_MemorySeat_Table_ID(string bodyStyle, string year)
        {
            switch(bodyStyle)
            {
                case "ZJ":
                    return 4174;
                case "LH":
                    return 4175;
                case "WJ":
                    return 4176;
                case "RS":
                    if (int.Parse(year) >= 2005) return 4354;
                    return 4177;
                case "CS":
                    if (int.Parse(year) >= 2005) return 4354;
                    else return 4333;
                case "R2": // D2???
                    return 4403;
            }
            return 0;
        }

        uint Get_DoorMux_Table_ID(string bodyStyle)
        {
            switch(bodyStyle)
            {
                case "WJ":
                    return 4179;
                case "ZJ":
                    return 4203;
                case "CS":
                    return 4336;
            }
            return 0;
        }

        uint Get_VehicleInfoCenter_Table_ID(string bodyStyle, string year)
        {
            switch (bodyStyle)
            {
                case "ZJ":
                    return 4056;
                case "WJ":
                    if (int.Parse(year) >= 2001) return 4243;
                    else return 4057;
                case "RS":
                case "RG":
                    return 4241;
                case "LH":
                    return 4242;
                case "AN":
                case "DN":
                    return 4304;
                case "DR":
                    return 4304;
                case "CS":
                    if (int.Parse(year) >= 2007) return 4370;
                    else return 4319;
                case "KJ":
                    return 4324;
            }
            return 0;
        }

        void LoadModuleID(uint moduleId)
        {
            selectedModule = drbdb.GetModule(moduleId);
            if(selectedModule == null)
            {
                MessageBox.Show("Attempted to load a bad module ID: " + moduleId);
                return;
            }
            listBox1.InvokeIfRequired(() =>
            {
                listBox1.Items.Clear();
                List<Function> modulesFunctions = drbdb.GetModuleFunctionsWithoutTX(selectedModule);
                Dictionary<uint, TXItem> FunctionIDToTXItem = new Dictionary<uint, TXItem>();
                foreach (TXItem item in selectedModule.TXItems.Where(tx => tx.Function != null))
                {
                    FunctionIDToTXItem[item.FunctionID] = item;
                }
                foreach (ModuleMenuItem moduleMenu in drbdb.GetModuleMenuItems())
                {
                    if (moduleMenu.ID == 1 || moduleMenu.ID == 4)
                    {
                        ListView targetBox = (moduleMenu.ID == 1 ? lstTests : lstActuators);
                        foreach (TXItem item in selectedModule.TXItems.Where(item => item.ModuleMenuID == moduleMenu.ID &&
                            item.Function != null && item.Function.LinkedFunctions.Any()))
                        {
                            TestObject test = new TestObject(item, GetFunctionTXChildren(FunctionIDToTXItem, item.Function).ToList());
                            targetBox.Items.Add(test.ToString()).Tag = test;
                        }
                        foreach (Function modMenuFunc in modulesFunctions.Where(func => func.ModuleMenuID == moduleMenu.ID))
                        {
                            TestObject test = new TestObject(modMenuFunc, GetFunctionTXChildren(FunctionIDToTXItem, modMenuFunc).ToList());
                            targetBox.Items.Add(test.ToString()).Tag = test;
                        }
                    }
                    else if(selectedModule.TXItems.Where(item => item.ModuleMenuID == moduleMenu.ID).Any())
                    {
                        lstDataMenus.Items.Add(moduleMenu);
                    }
                }
            });
        }

        IEnumerable<TXItem> GetFunctionTXChildren(Dictionary<uint, TXItem> FunctionIDToTXItem, Function func)
        {
            TXItem linkedItemTemp = null;
            foreach (Function linkedFunction in func.LinkedFunctions)
            {
                if (FunctionIDToTXItem.TryGetValue(linkedFunction.ID, out linkedItemTemp))
                {
                    yield return linkedItemTemp;
                }
            }
        }

        uint Get_Body_Table_ID(string year, string bodyStyle, string bcmStyle)
        {
            switch(bodyStyle)
            {
                case "LH":
                    if(GetIsTheftModuleCommunicating()) return 4098;
                    else return 4101;
                case "WJ":
                    if (GetIsTheftModuleCommunicating())
                    {
                        if (GetIsClimateModuleCommunicating()) return 4104;
                        else return 4105;
                    }
                    else
                    {
                        if (GetIsClimateModuleCommunicating()) return 4103;
                        else return 4102;
                    }
                case "AN":
                    if (int.Parse(year) >= 2001) return 4125;
                    else return 4106;
                case "AB":
                    return 4107;
                case "BR":
                case "BE":
                    return 4108;
                case "DN":
                    if (int.Parse(year) >= 2001) return 4125;
                    else return 4109;
                case "ZJ":
                    if(GetIsTheftModuleCommunicating()) return 4111;
                    else return 4110;
                case "JA":
                    if (bcmStyle == "Premium")
                    {
                        if (GetIsTheftModuleCommunicating()) return 4114;
                        else return 4113;
                    }
                    else return 4112;
                case "PROWLER": //body style code??
                    if(year.StartsWith("200")) return 4115;
                    else return 4123;
                case "JX":
                    if(bcmStyle == "Premium")
                    {
                        if(GetIsTheftModuleCommunicating()) return 4118;
                        else return 4117;
                    }
                    else return 4116;
                case "NS":
                case "GS":
                    if(bcmStyle == "Premium")
                    {
                        if(GetIsTheftModuleCommunicating()) return 4122;
                        else return 4121;
                    }
                    else if(bcmStyle == "Mid") return 4120;
                    else return 4119;
                case "JR":
                    if(bcmStyle == "Premium") return 4206;
                    else if(bcmStyle == "Mid") return 4205;
                    else return 4204; // base bcm
                case "RS":
                case "RG":
                    if(bcmStyle == "Premium")
                    {
                        if(GetIsTheftModuleCommunicating()) return 4210;
                        else return 4209;
                    }
                    else if(bcmStyle == "Mid") return 4208;
                    else return 4207;
                case "KJ":
                    return 4272;
                case "ZB":
                    if(year == "2008") return 4397;
                    else return 4313;
                case "CS":
                    return 4328;
            }
            return 0;
        }

        bool GetIsClimateModuleCommunicating()
        {
            if(connectedProtocol == Protocol.J1850)
            {
                List<byte> j1850Response = SendMessageAndGetResponse(0x24, 0x98, 0x22, 0x24, 0x00, 0x00);
                if (j1850Response.Count > 0) return true;
            }
            //no matter what, try CCD as a backup
            List<byte> ccdResponse = SendMessageAndGetResponse(0xB2, 0x98, 0x24, 0x01, 0x00);
            return ccdResponse.Count> 0;
        }

        bool GetIsTheftModuleCommunicating()
        {
            if (connectedProtocol == Protocol.J1850)
            {
                List<byte> j1850Response = SendMessageAndGetResponse(0x24, 0xA0, 0x22, 0x24, 0x00, 0x00);
                if (j1850Response.Count > 0) return true;
            }
            //no matter what, try CCD as a backup
            List<byte> ccdResponse = SendMessageAndGetResponse(0xB2, 0xA0, 0x24, 0x00, 0x00);
            return ccdResponse.Count > 0;
        }

        void GetBodyStyleAndYearFromBCM(out string bodyStyle, out string year)
        {
            bodyStyle = year = null;
            connectedProtocol = Protocol.CCD;
            ConnectChannel(ArduinoCommChannel.CCD);
            List<byte> bodyResp = SendMessageAndGetResponse(0xB2, 0x20, 0x24, 0x01, 0x00);
            if (bodyResp.Count == 5)
            {
                year = string.Format("19{0:X2}", bodyResp[3]);
                bodyStyle = GetBodyStyleFromBytes_CCD(bodyResp[4]);
            }
            else // try J1850
            {
                connectedProtocol = Protocol.J1850;
                ConnectChannel(ArduinoCommChannel.J1850);
                bodyResp = SendMessageAndGetResponse(0x24, 0x40, 0x22, 0x28, 0x00, 0x00);
                if (bodyResp.Count != 5) return;
                bodyStyle = GetBodyStyleFromBytes_PCI(bodyResp[3]);

                bodyResp = SendMessageAndGetResponse(0x24, 0x40, 0x22, 0x28, 0x01, 0x00);
                year = string.Format("{0:X2}{1:X2}", bodyResp[3], bodyResp[4]);
                if (bodyResp.Count != 5) return;
            }
        }

        void GetBodyStyleAndYearFromCluster(out string bodyStyle, out string year)
        {
            bodyStyle = year = null;
            connectedProtocol = Protocol.CCD;
            ConnectChannel(ArduinoCommChannel.CCD);
            List<byte> bodyResp = SendMessageAndGetResponse(0xB2, 0x20, 0x24, 0x01, 0x00);
            if (bodyResp.Count == 5)
            {
                year = string.Format("19{0:X2}", bodyResp[4]);
                bodyStyle = GetBodyStyleFromBytes_CCD(bodyResp[5]);
            }
            else // try J1850
            {
                connectedProtocol = Protocol.J1850;
                ConnectChannel(ArduinoCommChannel.J1850);
                bodyResp = SendMessageAndGetResponse(0x24, 0x60, 0x22, 0x28, 0x00, 0x00);
                if (bodyResp.Count != 5) return;
                bodyStyle = GetBodyStyleFromBytes_PCI(bodyResp[3]);

                bodyResp = SendMessageAndGetResponse(0x24, 0x60, 0x22, 0x28, 0x01, 0x00);
                year = string.Format("{0:X2}{1:X2}", bodyResp[3], bodyResp[4]);
                if (bodyResp.Count != 5) return;
            }
        }

        string GetBodyStyleFromBytes_PCI(byte body)
        {
            switch(body)
            {
                case 1:
                    return "RS";
                case 3:
                    return "RG";
                case 4:
                    return "TJ";
                case 5:
                    return "ZJ";
                case 6:
                    return "DR";
                case 7:
                    return "KJ";
                case 8:
                    return "AN";
                case 9:
                    return "LH";
                case 10:
                    return "JR";
                case 11:
                    return "LX";
                case 12:
                    return "JX";
                case 13:
                    return "WJ";
                case 14:
                    return "DN";
                case 15:
                    return "ANS4";
                case 16:
                    return "PL41";
                case 17:
                    return "PL44";
                case 18:
                    return "PT";
                case 19:
                    return "RS EV";
                case 21:
                    return "AB";
                default:
                    return "Undefined";
            }
        }

        string GetBodyStyleFromBytes_CCD(byte body)
        {
            switch(body)
            {
                case 1:
                    return "NS";
                case 2:
                    return "S Diesel";
                case 3:
                    return "GS";
                case 4:
                    return "YJ";
                case 5:
                    return "ZJ";
                case 6:
                    return "T300";
                case 7:
                    return "XJ";
                case 8:
                    return "AN";
                case 9:
                    return "LH";
                case 10:
                    return "JA";
                case 11:
                    return "LX";
                case 12:
                    return "JX";
                default:
                    return "Undefined";
            }
        }

        string GetBCMModuleLevel_CCD()
        {
            List<byte> bodyResp = SendMessageAndGetResponse(0xB2, 0x20, 0x24, 0x02, 0x00);
            if(bodyResp.Count != 5)
            {
                return null;
            }
            switch(bodyResp[4] & 3)
            {
                case 0:
                default:
                    return "Base";
                case 1:
                    return "Mid";
                case 2:
                    return "Premium";
            }
        }

        List<byte> SendMessageAndGetResponse(params byte[] message)
        {
            string messageAscii = string.Empty;
            for(int i=0; i < message.Length; i++)
            {
                messageAscii += string.Format("{0:X2}", message[i]);
            }
            messageAscii += '\n';
            responseReceived.Reset();
            response.Clear();
            for (int retry = 0; retry < 3; retry++)
            {
                arduino.Write(messageAscii);
                txtSerialLog.BeginInvoke((Action)(() => txtSerialLog.AppendText("(TX) " + messageAscii)));
                if (responseReceived.WaitOne(500))
                {
                    return new List<byte>(response);
                }
            }
            return new List<byte>();
        }

        //private string AutodetectArduinoPort()
        //{
        //    ManagementScope connectionScope = new ManagementScope();
        //    SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_SerialPort");
        //    ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);

        //    try
        //    {
        //        foreach (ManagementObject item in searcher.Get())
        //        {
        //            string desc = item["Description"].ToString();
        //            string deviceId = item["DeviceID"].ToString();

        //            if (desc.Contains("Arduino"))
        //            {
        //                return deviceId;
        //            }
        //        }
        //    }
        //    catch (ManagementException e)
        //    {
        //        /* Do Nothing */
        //    }

        //    return null;
        //}

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1 || listBox1.SelectedItem == null || listBox1.SelectedItem.GetType() != typeof(ModuleType)) return;
            ModuleType type = (ModuleType)listBox1.SelectedItem;
            Task.Factory.StartNew(() => ConnectModuleType(type));
        }

        void StopQuerying()
        {
            if (queryTask != null)
            {
                queryTaskStopSignal.Set();
                queryTask.Wait();
                queryTask.Dispose();
                queryTask = null;
            }
        }

        private void lstDataMenus_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstDataMenus.SelectedItem == null) return;
            ModuleMenuItem moduleMenu = (ModuleMenuItem)lstDataMenus.SelectedItem;
            StopQuerying();
            lstDataMenuTXs.InvokeIfRequired(() =>
            {
                lstDataMenuTXs.Items.Clear();
                visibleTxItems = selectedModule.TXItems.Where(item => item.ModuleMenuID == moduleMenu.ID).ToList();
                foreach (TXItem tx in visibleTxItems)
                {
                    ListViewItem newItem = new ListViewItem(tx.Name.ResourceString) { Tag = tx };
                    newItem.SubItems.Add(string.Empty);
                    lstDataMenuTXs.Items.Add(newItem);
                }
            });
            queryTask = new Task(QueryThread);
            queryTask.Start();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //StopQuerying();
            //lstDataMenuTXs.Items.Clear();
        }
    }

    public static class Extensions
    {
        public static uint TestModuleQualifiers(this Module module, string engineSize, string year, string bodyStyle, string ecmStyle)
        {
            if (module.Name == null) return 0;
            string modName = module.Name.ResourceString.
            Replace('(', ' ').
            Replace(')', ' ').
            Replace('/', ' ').
            Replace("SBEC3", "SBECIII").
            Replace("SBEC 3A", "SBECIIIA").
            Replace("Prowler", "PR").
            Replace("Dakota", "AN").
            Replace("Viper", "SR").
            Replace("FOUR-CYL", "2.5");
            uint count = 0;
            List<string> engineSizes = new List<string>();
            List<string> bodyStyles = new List<string>();
            foreach (string token in modName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (token.Length >= 3 && token[1] == '.')
                {
                    engineSizes.Add(token);
                }
                else if (token.Length == 2 && char.IsLetter(token[0]) && char.IsLetter(token[1]))
                {
                    bodyStyles.Add(token);
                }
                else if (token.Length == 2 && char.IsDigit(token[0]) && char.IsDigit(token[1]))
                {
                    if (token[0] != '9' && token[0] != '0') //engine size
                    {
                        engineSizes.Add(token[0] + "." + token[1]);
                    }
                    else if (year.Substring(2) == token)
                    {
                        count++;
                    }
                }
                else if (token.Contains("SBEC") || token.Contains("JTEC") || token.Contains("Siemens"))
                {
                    if (ecmStyle == token) count++;
                }
            }
            if (modName.Contains("18PL"))
            {
                engineSizes.Add("1.8");
                bodyStyles.Add("PL");
            }
            if (engineSizes.Count == 0)
            {
                if (modName.Contains("V6") && engineSize.Contains("V6")) count++;
                if (modName.Contains("V10") && engineSize.Contains("V10")) count++;
            }
            if (engineSizes.Any())
            {
                bool anyEnginesMatched = false;
                foreach (string engineSizeComp in engineSizes)
                {
                    if (engineSizeComp == "5.9")
                    {
                        if (modName.Contains("DIESEL"))
                        {
                            if (engineSize == "5.9L I6") anyEnginesMatched = true;
                        }
                        else if (engineSize == "5.9L V8") anyEnginesMatched = true;
                    }
                    else
                    {
                        if (engineSize.StartsWith(engineSizeComp))
                        {
                            if (modName.Contains("V6"))
                            {
                                if (engineSize.Contains("V6")) anyEnginesMatched = true;
                            }
                            else if (modName.Contains("V10"))
                            {
                                if (engineSize.Contains("V10")) anyEnginesMatched = true;
                            }
                            else
                            {
                                anyEnginesMatched = true;
                            }
                        }
                    }
                }
                if (anyEnginesMatched) count++;
                else return 0;
            }
            if (bodyStyles.Any())
            {
                bool anyBodyMatched = false;
                foreach (string bodyStyleComp in bodyStyles)
                {
                    if (bodyStyle == bodyStyleComp.ToUpper()) anyBodyMatched = true;
                }
                if (anyBodyMatched) count++;
                else return 0;
            }
            return count;
        }
    }
}
