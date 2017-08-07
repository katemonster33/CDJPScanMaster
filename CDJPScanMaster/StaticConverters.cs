﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CDJPScanMaster
{
    internal class StaticConverters
    {
        Dictionary<string, Enumeration> EnumerationsByName = new Dictionary<string, Enumeration>();
        internal StaticConverters()
        {
            XmlDocument staticCfgXml = new XmlDocument();
            staticCfgXml.Load("StaticConverters.xml");
            foreach(XmlNode enumNode in staticCfgXml.LastChild)
            {
                Enumeration en = new Enumeration() { Name = enumNode.Attributes["Name"].Value };
                foreach(XmlNode enumItemNode in enumNode)
                {
                    EnumItem ei = new EnumItem() { Name = enumItemNode.Attributes["Name"].Value };
                    if(enumNode.Attributes["Value"] != null)
                    {
                        ei.Value = int.Parse(enumNode.Attributes["Value"].Value);
                    }
                    if (enumNode.Attributes["Bit"] != null)
                    {
                        ei.Bit = int.Parse(enumNode.Attributes["Bit"].Value);
                    }
                    en.Enums.Add(ei);
                }
                EnumerationsByName[en.Name] = en;
            }
        }

        internal string RunConversionAndGetResult(string conversionName, int value)
        {
            Enumeration enumTemp = null;
            if (!EnumerationsByName.TryGetValue(conversionName, out enumTemp)) return "(BARF)";
            foreach(EnumItem ei in enumTemp.Enums)
            {
                if (ei.IsMatch(value)) return ei.Name;
            }
            return "(BARF)";
        }

        class Enumeration
        {
            public string Name;
            public List<EnumItem> Enums = new List<EnumItem>();
        }
        class EnumItem
        {
            public string Name;
            public int? Value = null;
            public int? Bit = null;

            public bool IsMatch(int compVal)
            {
                if (Value != null) return compVal == Value.Value;
                else if (Bit != null) return (compVal & (1 << Bit.Value)) == Bit.Value;
                else return false;
            }
        }
    }
}
