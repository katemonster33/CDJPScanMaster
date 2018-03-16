using ScanMaster.Database;
using ScanMaster.Database.Enums;
using ScanMaster.Database.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScanMaster.Comms
{
    public class VehicleLinkManager : IDisposable
    {
        ArduinoCommHandler arduino;
        Task queryTask;
        ManualResetEvent queryTaskStopSignal = new ManualResetEvent(false);
        Dictionary<string, string> cachedDatas = new Dictionary<string, string>();
        DRBDatabase database;
        StaticConverters converters = new StaticConverters();
        public VehicleLinkManager()
        {
            database = new DRBDatabase();
        }

        public DRBDatabase GetDatabase()
        {
            return database;
        }

        public bool OpenComms(string serialPort)
        {
            arduino = ArduinoCommHandler.CreateCommHandler(serialPort);
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
                    else if (tx.DataAcquisitionMethod == null) continue;
                    if (tx.DataAcquisitionMethod.Protocol == Protocol.SCI)
                    {
                        if (!isHighSpeedSciMode && tx.TransmitBytes[0] >= 0xF0)
                        {
                            isHighSpeedSciMode = arduino.SendMessageAndGetResponse(Protocol.SCI, 0x12).Count == 1;
                            Debug.Assert(isHighSpeedSciMode);
                            arduino.SendCommand(CommandID.SCI_SetHiSpeed);
                            Thread.Sleep(50);
                        }
                        if (isHighSpeedSciMode && tx.TransmitBytes[0] < 0xF0)
                        {
                            arduino.SendMessageAndGetResponse(Protocol.SCI, 0xFE);
                            Thread.Sleep(250);
                            arduino.SendCommand(CommandID.SCI_SetLoSpeed);
                            isHighSpeedSciMode = false;
                        }
                    }
                    byte[] xmitTemp = new byte[tx.DataAcquisitionMethod.RequestLen];
                    Array.Copy(tx.TransmitBytes, xmitTemp, tx.DataAcquisitionMethod.RequestLen);
                    List<byte> response = arduino.SendMessageAndGetResponse(tx.DataAcquisitionMethod.Protocol, xmitTemp);
                    if (response.Count == tx.DataAcquisitionMethod.ResponseLen)
                    {
                        byte[] dataBytes = tx.DataAcquisitionMethod.ExtractData(response.ToArray());
                        tx.DataDisplay.RawData = dataBytes;
                    }
                }
            }
            if (isHighSpeedSciMode)
            {
                arduino.SendMessageAndGetResponse(Protocol.SCI, 0xFE);
                Thread.Sleep(250);
                arduino.SendCommand(CommandID.SCI_SetLoSpeed);
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
            Protocol detectedProtocol = Protocol.Invalid;
            switch ((ModuleTypeID)moduleToConnect.TypeID)
            {
                case ModuleTypeID.Engine:
                    string engineSize = string.Empty, ecmType = string.Empty;
                    arduino.SendCommand(CommandID.Mux_SetSCIAEngine);
                    if (!GetEngineConfigSCI(ref engineSize, ref year, ref bodyStyle, ref ecmType))
                    {
                        arduino.SendCommand(CommandID.Mux_SetSCIBEngine);
                        if (!GetEngineConfigSCI(ref engineSize, ref year, ref bodyStyle, ref ecmType))
                        {
                            throw new Exception("Failed to identify engine over SCI A or SCI B configuration.");
                        }
                    }
                    IEnumerable<Module> engineModules = database.GetModules().Where(mod => mod.ModuleTypeID == (uint)ModuleTypeID.Engine);
                    uint engineModuleId = TableIDUtility.Get_Engine_Table_ID(engineModules, engineSize, year, bodyStyle, ecmType);
                    if (engineModuleId == 0)
                    {
                        throw new Exception("Engine appears to be unsupported.\n" +
                            "Engine size: " + engineSize + "\n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle + "\n" +
                            "ECM Type: " + ecmType);
                    }
                    return database.GetModule(engineModuleId);

                case ModuleTypeID.Transmission:
                    throw new Exception("Not Supported.");

                case ModuleTypeID.Body:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year, out detectedProtocol);
                    if (bodyStyle == null || year == null)
                    {
                        throw new Exception("Could not ID body module. Verify your connection.");
                    }
                    string level = GetBCMModuleLevel_CCD();
                    if (level == null)
                    {
                        level = "Base"; //it's possible the BCM doesn't support this command ID - in that case, it's likely just a base module.
                    }
                    uint moduleId = TableIDUtility.Get_Body_Table_ID(year, bodyStyle, level, 
                        () => GetIsTheftModuleCommunicating(detectedProtocol), 
                        () => GetIsClimateModuleCommunicating(detectedProtocol));
                    if (moduleId == 0)
                    {
                        throw new Exception("This combination of BCM appears to be unsupported. \n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle + "\n" +
                            "BCM Style: " + level);
                    }
                    return database.GetModule(moduleId);

                case ModuleTypeID.Brakes:
                    throw new Exception("Not supported");

                case ModuleTypeID.AudioSystems:
                    Protocol radioProtocol = Protocol.Invalid;
                    string radioModel = GetRadioModel(out radioProtocol);
                    if (string.IsNullOrEmpty(radioModel))
                    {
                        throw new Exception("Failed to identify audio module. Note that this is normal if a factory radio is installed.");
                    }
                    uint radioTableId = TableIDUtility.Get_Radio_Table_ID(radioModel, radioProtocol);
                    if (radioTableId == 0)
                    {
                        throw new Exception("Identified radio was not supported: " + radioModel);
                    }
                    return database.GetModule(radioTableId);

                case ModuleTypeID.VehicleTheftSecurity:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year, out detectedProtocol);
                    if (bodyStyle == null || year == null)
                    {
                        throw new Exception("Could not ID body module. Verify your connection.");
                    }
                    bool isImmob = true; // USE THIS FOR DETERMINING COMM LATER
                    uint vtssTableId = TableIDUtility.Get_VTSS_Table_ID(year, bodyStyle, isImmob);
                    if (vtssTableId == 0)
                    {
                        throw new Exception("This theft module appears to be unsupported. \n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle + "\n" +
                            "Is Immobilizer: " + isImmob.ToString());
                    }
                    return database.GetModule(vtssTableId);

                case ModuleTypeID.AirTempControl:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year, out detectedProtocol);
                    if (bodyStyle == null || year == null)
                    {
                        throw new Exception("Could not ID body module. Verify your connection.");
                    }
                    uint airTempTableId = TableIDUtility.Get_AirTempControl_Table_ID(bodyStyle);
                    if (airTempTableId == 0)
                    {
                        throw new Exception("This air temp control module appears to be unsupported. \n" +
                            "Body Style: " + bodyStyle);
                    }
                    return database.GetModule(airTempTableId);

                case ModuleTypeID.CompassMiniTrip:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year, out detectedProtocol);
                    if (bodyStyle == null || year == null)
                    {
                        throw new Exception("Could not ID body module. Verify your connection.");
                    }
                    uint compassMiniTripId = TableIDUtility.Get_CompassMiniTrip_Table_ID(bodyStyle, detectedProtocol);
                    if (compassMiniTripId == 0)
                    {
                        throw new Exception("This compass mini-trip module appears to be unsupported. \n" +
                            "Body Style: " + bodyStyle);
                    }
                    return database.GetModule(compassMiniTripId);

                case ModuleTypeID.MemorySeat:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year, out detectedProtocol);
                    if (bodyStyle == null || year == null)
                    {
                        throw new Exception("Could not ID body module. Verify your connection.");
                    }
                    uint memorySeatId = TableIDUtility.Get_MemorySeat_Table_ID(bodyStyle, year);
                    if (memorySeatId == 0)
                    {
                        throw new Exception("This compass mini-trip module appears to be unsupported. \n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle);
                    }
                    return database.GetModule(memorySeatId);

                case ModuleTypeID.InstrumentCluster:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year, out detectedProtocol);
                    if (bodyStyle == null || year == null)
                    {
                        throw new Exception("Could not ID body module. Verify your connection.");
                    }
                    uint clusterId = TableIDUtility.Get_MIC_Table_ID(bodyStyle, year, detectedProtocol);
                    if (clusterId == 0)
                    {
                        throw new Exception("This instrument cluster module appears to be unsupported. \n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle);
                    }
                    return database.GetModule(clusterId);

                case ModuleTypeID.VehicleInfoCenter:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year, out detectedProtocol);
                    if (bodyStyle == null || year == null)
                    {
                        throw new Exception("Could not ID body module. Verify your connection.");
                    }
                    uint vicId = TableIDUtility.Get_VehicleInfoCenter_Table_ID(bodyStyle, year);
                    if (vicId == 0)
                    {
                        throw new Exception("This vehicle info center module appears to be unsupported. \n" +
                            "Year: " + year + "\n" +
                            "Body Style: " + bodyStyle);
                    }
                    return database.GetModule(vicId);

                case ModuleTypeID.Otis:
                    GetBodyStyleAndYearFromBCM(out bodyStyle, out year, out detectedProtocol);
                    if (bodyStyle == null || year == null)
                    {
                        throw new Exception("Could not ID body module. Verify your connection.");
                    }
                    uint otisId = TableIDUtility.Get_OTIS_Table_ID(bodyStyle);
                    if (otisId == 0)
                    {
                        throw new Exception("This OTIS module appears to be unsupported. \n" +
                            "Body Style: " + bodyStyle);
                    }
                    return database.GetModule(otisId);
                default:
                    return null;
            }
        }

        bool GetEngineConfigSCI(ref string engineSize, ref string year, ref string bodyStyle, ref string ecuType)
        {
            // try SBECII
            List<byte> sciResp = arduino.SendMessageAndGetResponse(Protocol.SCI, 0x16, 0x81);
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
                sciResp = arduino.SendMessageAndGetResponse(Protocol.SCI, 0x16, 0x82);
                if (sciResp.Count != 2 || !arduino.GetSCIBytes(5, out sciResp)) return false;//not sure how to handle this, failure is the only way
                bodyStyle = converters.RunConversionAndGetResult("BodyStyle_SBECII", sciResp[0] | (sciResp[1] << 8) | (sciResp[2] << 16));
                return true;
            }
            else // try SBECIII / JTEC
            {
                byte mode2AData = 0;
                sciResp = arduino.SendMessageAndGetResponse(Protocol.SCI, 0x2A, 0x0B);
                if (sciResp.Count != 2 || !arduino.GetSCIByte(ref mode2AData)) return false;

                year = (1990 + mode2AData).ToString();
                sciResp = arduino.SendMessageAndGetResponse(Protocol.SCI, 0x2A, 0x0C);
                if (sciResp.Count != 2 || !arduino.GetSCIByte(ref mode2AData)) return false;
                engineSize = converters.RunConversionAndGetResult("EngineSize_SBECIII_JTEC", mode2AData);
                sciResp = arduino.SendMessageAndGetResponse(Protocol.SCI, 0x2A, 0x0F);
                if (sciResp.Count != 2 || !arduino.GetSCIByte(ref mode2AData)) return false;
                ecuType = converters.RunConversionAndGetResult("EngineControllerType_SBECIII_JTEC", mode2AData);
                sciResp = arduino.SendMessageAndGetResponse(Protocol.SCI, 0x2A, 0x10);
                if (sciResp.Count != 2 || !arduino.GetSCIByte(ref mode2AData)) return false;
                bodyStyle = converters.RunConversionAndGetResult("BodyStyle_SBECIII_JTEC", mode2AData);
                return true;
            }
        }

        string GetRadioModel(out Protocol detectedProtocol)
        {
            detectedProtocol = Protocol.Invalid;
            List<byte> ccdResponse = arduino.SendMessageAndGetResponse(Protocol.CCD, 0xB2, 0x96, 0x24, 0x10, 0x00);
            if (ccdResponse.Count == 5)
            {
                string model = string.Empty;
                model += (char)ccdResponse[3];

                ccdResponse = arduino.SendMessageAndGetResponse(Protocol.CCD, 0xB2, 0x96, 0x24, 0x11, 0x00);
                if (ccdResponse.Count != 5) return string.Empty;
                model += (char)ccdResponse[3];

                ccdResponse = arduino.SendMessageAndGetResponse(Protocol.CCD, 0xB2, 0x96, 0x24, 0x12, 0x00);
                if (ccdResponse.Count != 5) return string.Empty;
                model += (char)ccdResponse[3];
                return model;
            }
            else
            {
                List<byte> j1850Response = arduino.SendMessageAndGetResponse(Protocol.J1850, 0x24, 0x80, 0x22, 0x20, 0x01, 0x00);
                if (j1850Response.Count != 6) return string.Empty;
                return Encoding.ASCII.GetString(j1850Response.ToArray(), 3, 3);
            }
        }

        bool GetIsClimateModuleCommunicating(Protocol bcmProtocol)
        {
            if (bcmProtocol == Protocol.J1850)
            {
                List<byte> j1850Response = arduino.SendMessageAndGetResponse(Protocol.J1850, 0x24, 0x98, 0x22, 0x24, 0x00, 0x00);
                if (j1850Response.Count > 0) return true;
            }
            //no matter what, try CCD as a backup
            List<byte> ccdResponse = arduino.SendMessageAndGetResponse(Protocol.CCD, 0xB2, 0x98, 0x24, 0x01, 0x00);
            return ccdResponse.Count > 0;
        }

        string GetBCMModuleLevel_CCD()
        {
            List<byte> bodyResp = arduino.SendMessageAndGetResponse(Protocol.CCD, 0xB2, 0x20, 0x24, 0x02, 0x00);
            if (bodyResp.Count != 5)
            {
                return null;
            }
            return converters.RunConversionAndGetResult("BodyControllerType", bodyResp[4] & 3);
        }

        bool GetIsTheftModuleCommunicating(Protocol bcmProtocol)
        {
            if (bcmProtocol == Protocol.J1850)
            {
                List<byte> j1850Response = arduino.SendMessageAndGetResponse(Protocol.J1850, 0x24, 0xA0, 0x22, 0x24, 0x00, 0x00);
                if (j1850Response.Count > 0) return true;
            }
            //no matter what, try CCD as a backup
            List<byte> ccdResponse = arduino.SendMessageAndGetResponse(Protocol.CCD, 0xB2, 0xA0, 0x24, 0x00, 0x00);
            return ccdResponse.Count > 0;
        }

        void GetBodyStyleAndYearFromBCM(out string bodyStyle, out string year, out Protocol detectedProtocol)
        {
            detectedProtocol = Protocol.Invalid;
            bodyStyle = year = null;
            List<byte> bodyResp = arduino.SendMessageAndGetResponse(Protocol.CCD, 0xB2, 0x20, 0x24, 0x01, 0x00);
            if (bodyResp.Count == 5)
            {
                year = string.Format("19{0:X2}", bodyResp[3]);
                bodyStyle = converters.RunConversionAndGetResult("BodyStyle_BCM_CCD", bodyResp[4]);
                detectedProtocol = Protocol.CCD;
            }
            else // try J1850
            {
                bodyResp = arduino.SendMessageAndGetResponse(Protocol.J1850, 0x24, 0x40, 0x22, 0x28, 0x00, 0x00);
                if (bodyResp.Count != 5) return;
                bodyStyle = converters.RunConversionAndGetResult("BodyStyle_BCM_PCI", bodyResp[3]);

                bodyResp = arduino.SendMessageAndGetResponse(Protocol.J1850, 0x24, 0x40, 0x22, 0x28, 0x01, 0x00);
                if (bodyResp.Count != 5) return;
                year = string.Format("{0:X2}{1:X2}", bodyResp[3], bodyResp[4]);
                detectedProtocol = Protocol.J1850;
            }
        }

        void GetBodyStyleAndYearFromCluster(out string bodyStyle, out string year)
        {
            bodyStyle = year = null;
            List<byte> bodyResp = arduino.SendMessageAndGetResponse(Protocol.CCD, 0xB2, 0x20, 0x24, 0x01, 0x00);
            if (bodyResp.Count == 5)
            {
                year = string.Format("19{0:X2}", bodyResp[4]);
                bodyStyle = converters.RunConversionAndGetResult("BodyStyle_BCM_CCD", bodyResp[5]);
            }
            else // try J1850
            {
                bodyResp = arduino.SendMessageAndGetResponse(Protocol.J1850, 0x24, 0x60, 0x22, 0x28, 0x00, 0x00);
                if (bodyResp.Count != 5) return;
                bodyStyle = converters.RunConversionAndGetResult("BodyStyle_BCM_PCI", bodyResp[3]);

                bodyResp = arduino.SendMessageAndGetResponse(Protocol.J1850, 0x24, 0x60, 0x22, 0x28, 0x01, 0x00);
                year = string.Format("{0:X2}{1:X2}", bodyResp[3], bodyResp[4]);
                if (bodyResp.Count != 5) return;
            }
        }
    }
}
