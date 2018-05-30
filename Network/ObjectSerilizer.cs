using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Web;
using System.IO;

namespace Network
{
    public static class ObjectSerilizer
    {
        public static string Serialize(ArrayList cmd, ArrayList obj = null, string eof = "")
        {
            string xml = "";
            xml += "<dataObj>";
            
            if(cmd != null)
            {
                if(cmd.Count > 0)
                {
                    foreach(var s in cmd)
                    {
                        if (cmd.IndexOf(s) <= 1)
                        {
                            var Name = s.ToString().Split(':');
                            var type = Name[1].GetType();
                            var typeName = type.Name.ToLowerInvariant();
                            xml += "<var n=\'" + Name[0].ToString().ToLowerInvariant() + "\' t=\'" + typeName.Substring(0, 1) + "\'>" + Name[1] +"</var>" + eof;
                        }
                    }
                }
            }
            if (!xml.Contains("<obj>"))
                xml += "<obj t=\'" + "o" + "\' o=\'" + "param" + "\'>" + eof;
            if (obj != null)
            {
                foreach (var s in obj)
                {
                    var Name = s.ToString().Split(':');
                    string str = Name[1];
                    int num = int.MinValue;

                    object realVal = Name[1];

                    if (str == "false")
                        realVal = (bool)false;
                    else if (str == "true")
                        realVal = (bool)true;

                    if(int.TryParse(Name[1], out num))
                    {
                        realVal = num;
                    }

                    var type = realVal.GetType();
                    var typeName = type.Name.ToLowerInvariant();
                    if (typeName.ToLowerInvariant() == "int32")
                        typeName = "number";
                    if (typeName == "boolean" || typeName == "number" || typeName == "string" || typeName == "null")
                    {
                        xml += "<var n=\'" + Name[0] + "\' t=\'" + typeName[0] + "\'>" + Name[1].ToString() + "</var>" + eof;
                    }
                }
            }
            xml += "</obj>";
            xml += "</dataObj>";
            return xml;
        }
        public static string DecodeEntities(string text)
        {
            string decodedTxt = text.Replace("<![CDATA[", "").Replace("]]>", "");
            return decodedTxt;
        }
    }
}
