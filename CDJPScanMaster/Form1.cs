using DRBDB;
using DRBDB.Enums;
using DRBDB.Objects;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CDJPScanMaster
{
    public partial class Form1 : Form
    {
        SerialPort arduino;
        bool connected = true;
        Protocol connectedProtocol = Protocol.CCD;
        Module selectedModule = null;
        ManualResetEvent responseReceived = new ManualResetEvent(false);
        List<byte> response = new List<byte>();
        char[] serialBuffer = new char[100];
        int serialBufferLen = 0;
        Database drbdb = new Database();
        public Form1()
        {
            InitializeComponent();
            foreach(ModuleType type in drbdb.GetModuleTypes())
            {
                listBox1.Items.Add(type);
            }
            listBox1.Tag = drbdb.GetModuleTypes();
            string[] comPorts = SerialPort.GetPortNames();
            arduino = new SerialPort(comPorts[0], 115200, Parity.None, 8, StopBits.One);
            arduino.DataReceived += Arduino_DataReceived;
        }

        private void Arduino_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string buf = arduino.ReadExisting();
            for (int i = 0; i < buf.Length; i++)
            {
                serialBuffer[serialBufferLen] = buf[i];
                serialBufferLen++;
                if(buf[i] == '\n')
                {
                    ProcessMessage(new string(serialBuffer));
                    serialBufferLen = 0;
                }
            }
        }

        void ProcessMessage(string message)
        {
            List<byte> messageHex = new List<byte>();
            for (int i = 0; i < message.Length; i += 2)
            {
                messageHex.Add(readHex(message[i], message[i + 1]));
            }
            if (messageHex.Count > 1)
            {
                bool isResponse = false;
                if (connectedProtocol == Protocol.CCD || connectedProtocol == Protocol.CCD_2)
                {
                    isResponse = (messageHex[0] == 0xB2 && messageHex.Count == 5);
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

        private void listBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedValue == null) return;
            if(listBox1.SelectedValue.GetType() == typeof(ModuleType))
            {
                ModuleType moduleToConnect = (ModuleType)listBox1.SelectedValue;
                ConnectModuleType(moduleToConnect);
            }
        }

        void ConnectModuleType(ModuleType moduleToConnect)
        {
            string year, bodyStyle;
            switch ((ModuleTypeID)moduleToConnect.ID)
            {
                case ModuleTypeID.Engine:
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
                    {
                        uint moduleId = Get_Body_Table_ID(year, bodyStyle, level);
                        if (moduleId == 0)
                        {
                            MessageBox.Show("This combination of BCM appears to be unsupported. \n" +
                                "Year: " + year + "\n" +
                                "Body Style: " + bodyStyle + "\n" +
                                "BCM Style: " + level);
                            return;
                        }
                    }
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
                List<Function> moduleFunctions = drbdb.GetModuleFunctionsWithoutTX(selectedModule);
                foreach (ModuleMenuItem mmi in drbdb.GetModuleMenuItems())
                {

                }
            });
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
            List<byte> bodyResp = SendMessageAndGetResponse(0xB2, 0x20, 0x24, 0x01, 0x00);
            if (bodyResp.Count == 5)
            {
                year = string.Format("19{0:X2}", bodyResp[4]);
                bodyStyle = GetBodyStyleFromBytes_CCD(bodyResp[5]);
            }
            else // try J1850
            {
                connectedProtocol = Protocol.J1850;
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
            List<byte> bodyResp = SendMessageAndGetResponse(0xB2, 0x20, 0x24, 0x01, 0x00);
            if (bodyResp.Count == 5)
            {
                year = string.Format("19{0:X2}", bodyResp[4]);
                bodyStyle = GetBodyStyleFromBytes_CCD(bodyResp[5]);
            }
            else // try J1850
            {
                connectedProtocol = Protocol.J1850;
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
                messageAscii += fromHex((byte)(message[i] >> 4)) + fromHex((byte)(message[i] & 0x0F));
            }
            messageAscii += '\n';
            responseReceived.Reset();
            response.Clear();
            for (int retry = 0; retry < 3; retry++)
            {
                arduino.Write(messageAscii);
                if (responseReceived.WaitOne(500))
                {
                    break;
                }
            }
            return response;
        }

        char fromHex(byte nibble)
        {
            if (nibble >= 0 && nibble <= 9)
            {
                return (char)('0' + nibble);
            }
            else if (nibble >= 0xA && nibble <= 0xF)
            {
                return (char)('A' + (nibble - 0xA));
            }
            else throw new Exception();

        }
    }
}
