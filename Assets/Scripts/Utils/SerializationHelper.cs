using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Utils
{
    public class SerializationHelper
    {
        /// <summary>
        /// Serializes an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializableObject"></param>
        /// <param name="fileName"></param>
        public static void SerializeObject<T>(T serializableObject, string fileName)
        {
        }


        /// <summary>
        /// Deserializes an xml file into an object list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static object DeSerializeObject<T>(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) { return default; }

            T objectOut;

            if (!File.Exists(Application.persistentDataPath + "\\" + fileName))
                return null;

            try
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(Application.persistentDataPath + "\\" + fileName);
                var xmlString = xmlDocument.OuterXml;

                using (var read = new StringReader(xmlString))
                {
                    var outType = typeof(T);

                    var serializer = new XmlSerializer(outType);
                    using (XmlReader reader = new XmlTextReader(read))
                    {
                        objectOut = (T)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Exception while deserializing " + fileName);
                Debug.Log(ex);
                return null;
            }

            return objectOut;
        }
    }
}