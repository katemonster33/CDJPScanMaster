using DRBDB;
using DRBDB.Enums;
using DRBDB.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CDJPScanMaster
{
    internal class VehicleLinkManager : IDisposable
    {
        ArduinoCommHandler arduino;
        TextBoxLogger logger;
        Task queryTask;
        ManualResetEvent queryTaskStopSignal = new ManualResetEvent(false);
        Database database;
        StaticConverters converters = new StaticConverters();
        internal VehicleLinkManager(TextBoxLogger serialLogger)
        {
            logger = serialLogger;
            database = new Database();
        }

        public bool Open(string serialPort)
        {
            arduino = ArduinoCommHandler.CreateCommHandler(serialPort, logger);
            if (arduino == null) return false;
            if (!arduino.EstablishComms())
            {
                arduino.Dispose();
                arduino = null;
                return false;
            }
            queryTask = new Task(QueryThread);
            queryTask.Start();
            return true;
        }

        public void Dispose()
        {
            if (arduino != null) arduino.Dispose();
            if (queryTask != null)
            {
                queryTaskStopSignal.Set();
                queryTask.Wait();
                queryTask.Dispose();
            }
        }

        List<TXItem> visibleTxItems = new List<TXItem>();
        void QueryThread()
        {
            //listBoxUpdater.Enabled = true;
            queryTaskStopSignal.Reset();
            bool isHighSpeedSciMode = false;
            while (!queryTaskStopSignal.WaitOne(1))
            {
                for (int i = 0; i < visibleTxItems.Count; i++)
                {
                    TXItem tx = visibleTxItems[i];
                    if (tx.TransmitBytes.Length == 0 || tx.DataAcquisitionMethod == null || (tx.TransmitBytes.Length == 1 && tx.TransmitBytes[0] == 0)) continue;
                    if (arduino.ConnectedProtocol == Protocol.SCI)
                    {
                        if (!isHighSpeedSciMode && tx.TransmitBytes[0] >= 0xF0)
                        {
                            isHighSpeedSciMode = arduino.SendMessageAndGetResponse(0x12).Count == 1;
                            Debug.Assert(isHighSpeedSciMode);
                            Thread.Sleep(50);
                        }
                        if (isHighSpeedSciMode && tx.TransmitBytes[0] < 0xF0)
                        {
                            arduino.SendMessageAndGetResponse(0xFE);
                            Thread.Sleep(250);
                            isHighSpeedSciMode = false;
                        }
                    }
                    byte[] xmitTemp = new byte[tx.DataAcquisitionMethod.RequestLen];
                    Array.Copy(tx.TransmitBytes, xmitTemp, tx.DataAcquisitionMethod.RequestLen);
                    List<byte> response = arduino.SendMessageAndGetResponse(xmitTemp);
                    if (response.Count == tx.DataAcquisitionMethod.ResponseLen)
                    {
                        byte[] dataBytes = tx.DataAcquisitionMethod.ExtractData(response.ToArray());
                        tx.DataDisplay.RawData = dataBytes;
                    }
                }
            }
            if (isHighSpeedSciMode)
            {
                arduino.SendMessageAndGetResponse(0xFE);
                Thread.Sleep(250);
            }
            //listBoxUpdater.Enabled = false;
        }

        public void SetQueriedTXItems(List<TXItem> items)
        {
            lock(visibleTxItems)
            {
                visibleTxItems.Clear();
                visibleTxItems.AddRange(items);
            }
        }

        public Module ScanModuleType(ModuleType moduleToConnect)
        {
            string year = string.Empty, bodyStyle = string.Empty;
            switch ((ModuleTypeID)moduleToConnect.TypeID)
            {
                case ModuleTypeID.Engine:
                    string engineSize = string.Empty, ecmType = string.Empty;
                    arduino.ConnectChannel(ArduinoCommChannel.SCI_A_Engine);
                    if (!GetEngineConfigSCI(ref engineSize, ref year, ref bodyStyle, ref ecmType))
                    {
                        arduino.ConnectChannel(ArduinoCommChannel.SCI_B_Engine);
                        if (!GetEngineConfigSCI(ref engineSize, ref year, ref bodyStyle, ref ecmType))
                        {
                            MessageBox.Show("Failed to identify engine over SCI A or SCI B configuration.");
                            return null;
                        }
                    }
                    uint engineModuleId = Get_Engine_Table_ID(engineSize, year, bodyStyle, ecmType);
                    if (engineModuleId == 0)
                    {
                        MessageBox.Show("Engine appears to be unsupported.\n" +
                            "Engine size: " + engineSize + "\n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle + "\n" +
                            "ECM Type: " + ecmType);
                        return null;
                    }
                    return database.GetModule(engineModuleId);

                case ModuleTypeID.Transmission:
                    return null;

                case ModuleTypeID.Body:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return null;
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
                        return null;
                    }
                    return database.GetModule(moduleId);

                case ModuleTypeID.Brakes:
                    return null;

                case ModuleTypeID.AudioSystems:
                    string radioModel = GetRadioModel();
                    if (string.IsNullOrEmpty(radioModel))
                    {
                        MessageBox.Show("Failed to identify audio module. Note that this is normal if a factory radio is installed.");
                    }
                    uint radioTableId = Get_Radio_Table_ID(radioModel);
                    if (radioTableId == 0)
                    {
                        MessageBox.Show("Identified radio was not supported: " + radioModel);
                        return null;
                    }
                    return database.GetModule(radioTableId);

                case ModuleTypeID.VehicleTheftSecurity:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return null;
                    }
                    bool isImmob = true; // USE THIS FOR DETERMINING COMM LATER
                    uint vtssTableId = TableIDUtility.Get_VTSS_Table_ID(year, bodyStyle, isImmob);
                    if (vtssTableId == 0)
                    {
                        MessageBox.Show("This theft module appears to be unsupported. \n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle + "\n" +
                            "Is Immobilizer: " + isImmob.ToString());
                        return null;
                    }
                    return database.GetModule(vtssTableId);

                case ModuleTypeID.AirTempControl:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return null;
                    }
                    uint airTempTableId = TableIDUtility.Get_AirTempControl_Table_ID(bodyStyle);
                    if (airTempTableId == 0)
                    {
                        MessageBox.Show("This air temp control module appears to be unsupported. \n" +
                            "Body Style: " + bodyStyle);
                        return null;
                    }
                    return database.GetModule(airTempTableId);

                case ModuleTypeID.CompassMiniTrip:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return null;
                    }
                    uint compassMiniTripId = TableIDUtility.Get_CompassMiniTrip_Table_ID(bodyStyle, arduino.ConnectedProtocol);
                    if (compassMiniTripId == 0)
                    {
                        MessageBox.Show("This compass mini-trip module appears to be unsupported. \n" +
                            "Body Style: " + bodyStyle);
                    }
                    return database.GetModule(compassMiniTripId);

                case ModuleTypeID.MemorySeat:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return null;
                    }
                    uint memorySeatId = Get_MemorySeat_Table_ID(bodyStyle, year);
                    if (memorySeatId == 0)
                    {
                        MessageBox.Show("This compass mini-trip module appears to be unsupported. \n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle);
                        return null;
                    }
                    return database.GetModule(memorySeatId);

                case ModuleTypeID.InstrumentCluster:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return null;
                    }
                    uint clusterId = TableIDUtility.Get_MIC_Table_ID(bodyStyle, year, arduino.ConnectedProtocol);
                    if (clusterId == 0)
                    {
                        MessageBox.Show("This instrument cluster module appears to be unsupported. \n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle);
                        return null;
                    }
                    return database.GetModule(clusterId);

                case ModuleTypeID.VehicleInfoCenter:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return null;
                    }
                    uint vicId = Get_VehicleInfoCenter_Table_ID(bodyStyle, year);
                    if (vicId == 0)
                    {
                        MessageBox.Show("This vehicle info center module appears to be unsupported. \n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle);
                        return null;
                    }
                    return database.GetModule(vicId);

                case ModuleTypeID.Otis:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year);
                    if (bodyStyle == null || year == null)
                    {
                        MessageBox.Show("Could not ID body module. Verify your connection.");
                        return null;
                    }
                    uint otisId = TableIDUtility.Get_OTIS_Table_ID(bodyStyle);
                    if (otisId == 0)
                    {
                        MessageBox.Show("This OTIS module appears to be unsupported. \n" +
                            "Body Style: " + bodyStyle);
                        return null;
                    }
                    return database.GetModule(otisId);
                default:
                    return null;
            }
        }

        uint Get_Engine_Table_ID(string engineSize, string year, string bodyStyle, string ecmType)
        {
            Module mostQualifiedModule = null;
            uint maxQualifiers = 0;
            foreach (Module mod in database.GetModules().Where(mod => mod.ModuleTypeID == (uint)ModuleTypeID.Engine))
            {
                uint tempQualifiers = mod.TestModuleQualifiers(engineSize, year, bodyStyle, ecmType);
                if (tempQualifiers > maxQualifiers)
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
            // try SBECII
            List<byte> sciResp = arduino.SendMessageAndGetResponse(0x16, 0x81);
            if (sciResp.Count == 2 && arduino.GetSCIBytes(5, out sciResp))
            {
                year = (1990 + (sciResp[2] & 0x0F)).ToString();
                byte engineCfg = (byte)(sciResp[2] >> 4);
                byte manufacturer = (byte)((sciResp[3] >> 3) & 7);
                engineSize = converters.RunConversionAndGetResult("EngineSize_SBECII", engineCfg);
                if (engineSize == "5.9L") // 5.9 L, is it an I6 or V8?
                {
                    if (manufacturer == 6) //Cummins
                    {
                        engineSize += " I6";
                    }
                    else
                    {
                        engineSize += " V8";
                    }
                }
                ecuType = "SBEC/SBECII";
                sciResp = arduino.SendMessageAndGetResponse(0x16, 0x82);
                if (sciResp.Count != 2 || !arduino.GetSCIBytes(5, out sciResp)) return false;//not sure how to handle this, failure is the only way
                bodyStyle = converters.RunConversionAndGetResult("BodyStyle_SBECII", sciResp[0] | (sciResp[1] << 8) | (sciResp[2] << 16));
                return true;
            }
            else // try SBECIII / JTEC
            {
                byte mode2AData = 0;
                sciResp = arduino.SendMessageAndGetResponse(0x2A, 0x0B);
                if (sciResp.Count != 2 || !arduino.GetSCIByte(ref mode2AData)) return false;

                year = (1990 + mode2AData).ToString();
                sciResp = arduino.SendMessageAndGetResponse(0x2A, 0x0C);
                if (sciResp.Count != 2 || !arduino.GetSCIByte(ref mode2AData)) return false;
                engineSize = converters.RunConversionAndGetResult("EngineSize_SBECIII_JTEC", mode2AData);
                sciResp = arduino.SendMessageAndGetResponse(0x2A, 0x0F);
                if (sciResp.Count != 2 || !arduino.GetSCIByte(ref mode2AData)) return false;
                ecuType = converters.RunConversionAndGetResult("EngineControllerType_SBECIII_JTEC", mode2AData);
                sciResp = arduino.SendMessageAndGetResponse(0x2A, 0x10);
                if (sciResp.Count != 2 || !arduino.GetSCIByte(ref mode2AData)) return false;
                bodyStyle = converters.RunConversionAndGetResult("BodyStyle_SBECIII_JTEC", mode2AData);
                return true;
            }
        }

        string GetRadioModel()
        {
            arduino.ConnectChannel(ArduinoCommChannel.CCD);
            List<byte> ccdResponse = arduino.SendMessageAndGetResponse(0xB2, 0x96, 0x24, 0x10, 0x00);
            if (ccdResponse.Count == 5)
            {
                string model = string.Empty;
                model += (char)ccdResponse[3];

                ccdResponse = arduino.SendMessageAndGetResponse(0xB2, 0x96, 0x24, 0x11, 0x00);
                if (ccdResponse.Count != 5) return string.Empty;
                model += (char)ccdResponse[3];

                ccdResponse = arduino.SendMessageAndGetResponse(0xB2, 0x96, 0x24, 0x12, 0x00);
                if (ccdResponse.Count != 5) return string.Empty;
                model += (char)ccdResponse[3];
                return model;
            }
            else
            {
                arduino.ConnectChannel(ArduinoCommChannel.J1850);
                List<byte> j1850Response = arduino.SendMessageAndGetResponse(0x24, 0x80, 0x22, 0x20, 0x01, 0x00);
                if (j1850Response.Count != 6) return string.Empty;
                return Encoding.ASCII.GetString(j1850Response.ToArray(), 3, 3);
            }
        }

        uint Get_Radio_Table_ID(string radioModel)
        {
            switch (radioModel)
            {
                case "RBC":
                case "RBR":
                    return 4032;
                case "RAD":
                    return 4033;
                case "RAZ":
                    if (arduino.ConnectedProtocol == Protocol.J1850) return 4035;
                    else return 4034;
                case "RBN":
                    if (arduino.ConnectedProtocol == Protocol.J1850) return 4037;
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
            switch (bodyStyle)
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
            switch (bodyStyle)
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

        bool GetIsClimateModuleCommunicating()
        {
            if (arduino.ConnectedProtocol == Protocol.J1850)
            {
                List<byte> j1850Response = arduino.SendMessageAndGetResponse(0x24, 0x98, 0x22, 0x24, 0x00, 0x00);
                if (j1850Response.Count > 0) return true;
            }
            //no matter what, try CCD as a backup
            List<byte> ccdResponse = arduino.SendMessageAndGetResponse(0xB2, 0x98, 0x24, 0x01, 0x00);
            return ccdResponse.Count > 0;
        }



        uint Get_Body_Table_ID(string year, string bodyStyle, string bcmStyle)
        {
            switch (bodyStyle)
            {
                case "LH":
                    if (GetIsTheftModuleCommunicating()) return 4098;
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
                    if (GetIsTheftModuleCommunicating()) return 4111;
                    else return 4110;
                case "JA":
                    if (bcmStyle == "Premium")
                    {
                        if (GetIsTheftModuleCommunicating()) return 4114;
                        else return 4113;
                    }
                    else return 4112;
                case "PROWLER": //body style code??
                    if (year.StartsWith("200")) return 4115;
                    else return 4123;
                case "JX":
                    if (bcmStyle == "Premium")
                    {
                        if (GetIsTheftModuleCommunicating()) return 4118;
                        else return 4117;
                    }
                    else return 4116;
                case "NS":
                case "GS":
                    if (bcmStyle == "Premium")
                    {
                        if (GetIsTheftModuleCommunicating()) return 4122;
                        else return 4121;
                    }
                    else if (bcmStyle == "Mid") return 4120;
                    else return 4119;
                case "JR":
                    if (bcmStyle == "Premium") return 4206;
                    else if (bcmStyle == "Mid") return 4205;
                    else return 4204; // base bcm
                case "RS":
                case "RG":
                    if (bcmStyle == "Premium")
                    {
                        if (GetIsTheftModuleCommunicating()) return 4210;
                        else return 4209;
                    }
                    else if (bcmStyle == "Mid") return 4208;
                    else return 4207;
                case "KJ":
                    return 4272;
                case "ZB":
                    if (year == "2008") return 4397;
                    else return 4313;
                case "CS":
                    return 4328;
            }
            return 0;
        }

        string GetBCMModuleLevel_CCD()
        {
            List<byte> bodyResp = arduino.SendMessageAndGetResponse(0xB2, 0x20, 0x24, 0x02, 0x00);
            if (bodyResp.Count != 5)
            {
                return null;
            }
            return converters.RunConversionAndGetResult("BodyControllerType", bodyResp[4] & 3);
        }

        bool GetIsTheftModuleCommunicating()
        {
            if (arduino.ConnectedProtocol == Protocol.J1850)
            {
                List<byte> j1850Response = arduino.SendMessageAndGetResponse(0x24, 0xA0, 0x22, 0x24, 0x00, 0x00);
                if (j1850Response.Count > 0) return true;
            }
            //no matter what, try CCD as a backup
            List<byte> ccdResponse = arduino.SendMessageAndGetResponse(0xB2, 0xA0, 0x24, 0x00, 0x00);
            return ccdResponse.Count > 0;
        }

        void GetBodyStyleAndYearFromBCM(out string bodyStyle, out string year)
        {
            bodyStyle = year = null;
            arduino.ConnectChannel(ArduinoCommChannel.CCD);
            List<byte> bodyResp = arduino.SendMessageAndGetResponse(0xB2, 0x20, 0x24, 0x01, 0x00);
            if (bodyResp.Count == 5)
            {
                year = string.Format("19{0:X2}", bodyResp[3]);
                bodyStyle = converters.RunConversionAndGetResult("BodyStyle_BCM_CCD", bodyResp[4]);
            }
            else // try J1850
            {
                arduino.ConnectChannel(ArduinoCommChannel.J1850);
                bodyResp = arduino.SendMessageAndGetResponse(0x24, 0x40, 0x22, 0x28, 0x00, 0x00);
                if (bodyResp.Count != 5) return;
                bodyStyle = converters.RunConversionAndGetResult("BodyStyle_BCM_PCI", bodyResp[3]);

                bodyResp = arduino.SendMessageAndGetResponse(0x24, 0x40, 0x22, 0x28, 0x01, 0x00);
                year = string.Format("{0:X2}{1:X2}", bodyResp[3], bodyResp[4]);
                if (bodyResp.Count != 5) return;
            }
        }

        void GetBodyStyleAndYearFromCluster(out string bodyStyle, out string year)
        {
            bodyStyle = year = null;
            arduino.ConnectChannel(ArduinoCommChannel.CCD);
            List<byte> bodyResp = arduino.SendMessageAndGetResponse(0xB2, 0x20, 0x24, 0x01, 0x00);
            if (bodyResp.Count == 5)
            {
                year = string.Format("19{0:X2}", bodyResp[4]);
                bodyStyle = converters.RunConversionAndGetResult("BodyStyle_BCM_CCD", bodyResp[5]);
            }
            else // try J1850
            {
                arduino.ConnectChannel(ArduinoCommChannel.J1850);
                bodyResp = arduino.SendMessageAndGetResponse(0x24, 0x60, 0x22, 0x28, 0x00, 0x00);
                if (bodyResp.Count != 5) return;
                bodyStyle = converters.RunConversionAndGetResult("BodyStyle_BCM_PCI", bodyResp[3]);

                bodyResp = arduino.SendMessageAndGetResponse(0x24, 0x60, 0x22, 0x28, 0x01, 0x00);
                year = string.Format("{0:X2}{1:X2}", bodyResp[3], bodyResp[4]);
                if (bodyResp.Count != 5) return;
            }
        }
    }
}
