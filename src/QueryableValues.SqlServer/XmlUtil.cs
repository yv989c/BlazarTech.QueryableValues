#if EFCORE
using System;
using System.Collections.Generic;
using System.Text;

namespace BlazarTech.QueryableValues
{
    internal static class XmlUtil
    {
        public static string GetXml<T>(IEnumerable<T> values)
            where T : notnull
        {
            var sb = new StringBuilder();

            using (var stringWriter = new System.IO.StringWriter(sb))
            {
                var settings = new System.Xml.XmlWriterSettings
                {
                    ConformanceLevel = System.Xml.ConformanceLevel.Fragment
                };

                using var xmlWriter = System.Xml.XmlWriter.Create(stringWriter, settings);

                xmlWriter.WriteStartElement("R");

                foreach (var value in values)
                {
                    if (value is null)
                    {
                        continue;
                    }

                    xmlWriter.WriteStartElement("V");

                    if (value is DateTime dateTime)
                    {
                        xmlWriter.WriteValue(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified));
                    }
                    else if (value is Guid guid)
                    {
                        xmlWriter.WriteValue(guid.ToString());
                    }
                    else
                    {
                        xmlWriter.WriteValue(value);
                    }

                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement();
            }

            return sb.ToString();
        }
    }
}
#endif