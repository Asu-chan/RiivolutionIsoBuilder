using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ExtensionMethods
{
    public static class StringExtensions
    {
        public static int AsInt32(this string s, int _default)
        {
            if (s == "")
                return _default;
            else
                return Convert.ToInt32(s);
        }

        public static uint AsUInt32(this string s, uint _default)
        {
            if (s == "")
                return _default;
            else
            {
                uint result = 0;

                try
                {
                    result = Convert.ToUInt32(s);
                }
                catch (Exception e)
                {
                    try
                    {
                        if (s.Contains("0x"))
                            s = s.Substring(2);

                        result = Convert.ToUInt32(s, 16);
                    }
                    catch (Exception ee)
                    {
                        Console.WriteLine("Number " + s + " isn't base 10 nor 16");
                    }
                }

                return result;
            }
        }

        public static bool AsBool(this string s, bool _default)
        {
            if (s == "")
                return _default;
            else
                return Convert.ToBoolean(s);
        }

        public static List<byte> ReadHexString(this string s)
        {
            if (s.Length % 2 == 1)
            {
                Console.WriteLine("Odd number of digits in " + s);
                return new List<byte>();
            }

            if (s.Contains("0x"))
            {
                s = s.Substring(2);
            }

            List<byte> result = new List<byte>();
            while (s.Length > 0)
            {
                byte tmp;
                try
                {
                    tmp = Convert.ToByte(s.Substring(0, 2), 16);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Invalid Hex Number " + s);
                    return new List<byte>();
                }
                result.Add(tmp);

                s = s.Substring(2);
            }

            return result;
        }
    }

    public static class XmlNodeExtensions
    {
        public static XmlNodeList GetElementsByTagName(this XmlNode node, string name)
        {
            return ((XmlElement)node).GetElementsByTagName(name);
        }

        public static string Value(this XmlNode node)
        {
            return (node == null) ? "" : node.Value;
        }
    }

    public static class ByteArrayExtensions
    {
        public static string AsString(this byte[] array)
        {
            string s = "";
            foreach(byte b in array)
            {
                s += b.ToString("X2");
            }
            return s;
        }
    }

    public static class ByteListExtensions
    {
        public static string AsString(this List<byte> array)
        {
            string s = "";
            foreach(byte b in array)
            {
                s += b.ToString("X2");
            }
            return s;
        }
    }
}