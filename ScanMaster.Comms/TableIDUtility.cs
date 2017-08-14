using ScanMaster.Database.Enums;
using ScanMaster.Database.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanMaster.Comms
{
    static class TableIDUtility
    {

        internal static uint Get_SentryKeyModule_Table_ID(string year, Protocol connectedProtocol)
        {
            if (connectedProtocol == Protocol.CCD) return 4181;
            else
            {
                if (int.Parse(year) >= 2003) return 4327; //SKREEM
                else return 4182; //SKIM PCI
            }
        }

        internal static uint Get_VTSS_Table_ID(string year, string bodyStyle, bool isImmob)
        {
            switch (bodyStyle)
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

        internal static uint Get_AirTempControl_Table_ID(string bodyStyle)
        {
            switch (bodyStyle)
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

        internal static uint Get_MIC_Table_ID(string bodyStyle, string year, Protocol connectedProtocol)
        {
            switch (bodyStyle)
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

        internal static uint Get_OTIS_Table_ID(string bodyStyle)
        {
            if (bodyStyle == "LH") return 4055;
            else return 0;
        }

        internal static uint Get_CompassMiniTrip_Table_ID(string bodyStyle, Protocol connectedProtocol)
        {
            switch (bodyStyle)
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
        
        internal static uint Get_Body_Table_ID(string year, string bodyStyle, string bcmStyle, Func<bool> isTheftModuleOnlineCallback, Func<bool> isClimateModuleOnlineCallback)
        {
            switch (bodyStyle)
            {
                case "LH":
                    if (isTheftModuleOnlineCallback()) return 4098;
                    else return 4101;
                case "WJ":
                    if (isTheftModuleOnlineCallback())
                    {
                        if (isClimateModuleOnlineCallback()) return 4104;
                        else return 4105;
                    }
                    else
                    {
                        if (isClimateModuleOnlineCallback()) return 4103;
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
                    if (isTheftModuleOnlineCallback()) return 4111;
                    else return 4110;
                case "JA":
                    if (bcmStyle == "Premium")
                    {
                        if (isTheftModuleOnlineCallback()) return 4114;
                        else return 4113;
                    }
                    else return 4112;
                case "PROWLER": //body style code??
                    if (year.StartsWith("200")) return 4115;
                    else return 4123;
                case "JX":
                    if (bcmStyle == "Premium")
                    {
                        if (isTheftModuleOnlineCallback()) return 4118;
                        else return 4117;
                    }
                    else return 4116;
                case "NS":
                case "GS":
                    if (bcmStyle == "Premium")
                    {
                        if (isTheftModuleOnlineCallback()) return 4122;
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
                        if (isTheftModuleOnlineCallback()) return 4210;
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

        internal static uint Get_Radio_Table_ID(string radioModel, Protocol connectedProtocol)
        {
            switch (radioModel)
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

        internal static uint Get_TirePressureMonitor_Table_ID(string bodyStyle)
        {
            if (bodyStyle == "PROWLER") return 4173;
            else return 0;
        }

        internal static uint Get_MemorySeat_Table_ID(string bodyStyle, string year)
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

        internal static uint Get_DoorMux_Table_ID(string bodyStyle)
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

        internal static uint Get_VehicleInfoCenter_Table_ID(string bodyStyle, string year)
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

        internal static uint Get_Engine_Table_ID(IEnumerable<Module> engineModules, string engineSize, string year, string bodyStyle, string ecmType)
        {
            Module mostQualifiedModule = null;
            uint maxQualifiers = 0;
            foreach (Module mod in engineModules)
            {
                uint tempQualifiers = TestEngineModuleQualifiers(mod, engineSize, year, bodyStyle, ecmType);
                if (tempQualifiers > maxQualifiers)
                {
                    maxQualifiers = tempQualifiers;
                    mostQualifiedModule = mod;
                }
            }
            if (mostQualifiedModule == null) return 0;
            return mostQualifiedModule.ID;
        }

        static uint TestEngineModuleQualifiers(Module module, string engineSize, string year, string bodyStyle, string ecmStyle)
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
