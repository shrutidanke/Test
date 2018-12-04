using System;
using System.Collections.Generic;
using System.Web;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace VibeDirect
{

    public static class XmlExtension
    {
        // public static string Serialize<T>(this T value)
        public static string Serialize<T>(T value)
        {
            if (value == null) return string.Empty;

            var xmlSerializer = new XmlSerializer(typeof(T));

            using (var stringWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true }))
                {
                    xmlSerializer.Serialize(xmlWriter, value);
                    return stringWriter.ToString();
                }
            }
        }
    }
}