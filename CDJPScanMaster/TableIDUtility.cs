using DRBDB.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDJPScanMaster
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
    }
}
