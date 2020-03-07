using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Web;
using System.IO;
using System.Security.Cryptography;

namespace PPOProtocol
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

        public static string GenerateRandomString(int newLength)
        {
            const string loc5 = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var loc2 = loc5.ToCharArray();
            var loc3 = "";
            var random = new Random();
            for (var i = 0; i < newLength; ++i)
            {
                loc3 += loc2[(int)Math.Floor(random.NextDouble() * loc2.Length)];
            }
            return loc3;
        }

        public static string CalcMd5(string str)
        {
            var encodedPassword = new UTF8Encoding().GetBytes(str);

            // need MD5 to calculate the hash
            var hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(encodedPassword);
            // string representation (similar to UNIX format)
            var encoded = BitConverter.ToString(hash)
                // without dashes
                .Replace("-", string.Empty)
                // make lowercase
                .ToLower();

            return encoded;
        }

        public static string CalcSH1(string str)
        {
            var encodedPassword = new UTF8Encoding().GetBytes(str);

            // need SHA1 to calculate the hash
            var hash = new SHA1Managed().ComputeHash(encodedPassword);
            // string representation (similar to UNIX format)
            var encoded = BitConverter.ToString(hash)
                // without dashes
                .Replace("-", string.Empty)
                // make lowercase
                .ToLower();

            return encoded;
        }
    }
}
