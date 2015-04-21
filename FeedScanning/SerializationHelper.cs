// Type: FeedSkimming.SerializationHelper
// Assembly: FeedSkimmer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 68D455E3-3B61-4386-A8E3-410E9F903232
// Assembly location: D:\!Prasse\Downloads\FeedSkimmer\FeedSkimmer.exe

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;

namespace FeedScanning
{
  public class SerializationHelper
  {
    public static void Serialize(string fileName, object objectToSerialize, SerializationHelper.FileType fileType = SerializationHelper.FileType.XML)
    {
      using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
        SerializationHelper.Serialize((Stream) fileStream, objectToSerialize, fileType);
    }

    public static void Serialize(Stream serializedStream, object objectToSerialize, SerializationHelper.FileType fileType = SerializationHelper.FileType.XML)
    {
      switch (fileType)
      {
        case SerializationHelper.FileType.Binary:
          SerializationHelper.SerializeBinary(serializedStream, objectToSerialize);
          break;
        case SerializationHelper.FileType.XML:
          SerializationHelper.SerializeXML(serializedStream, objectToSerialize);
          break;
        default:
          throw new FormatException();
      }
    }

    public static T DeSerialize<T>(string fileName, SerializationHelper.FileType fileType = SerializationHelper.FileType.XML)
    {
      using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
      {
        using (new StreamXmlSanitizer((Stream) fileStream))
          return SerializationHelper.DeSerialize<T>((Stream) fileStream, fileType);
      }
    }

    public static T DeSerialize<T>(Stream serializedStream, SerializationHelper.FileType fileType = SerializationHelper.FileType.XML)
    {
      switch (fileType)
      {
        case SerializationHelper.FileType.Binary:
          return SerializationHelper.DeSerializeBinary<T>(serializedStream);
        case SerializationHelper.FileType.XML:
          return SerializationHelper.DeSerializeXML<T>(serializedStream);
        default:
          throw new FormatException();
      }
    }

    private static void SerializeBinary(Stream binaryStream, object objectToSerialize)
    {
      using (binaryStream)
        new BinaryFormatter().Serialize(binaryStream, objectToSerialize);
    }

    private static T DeSerializeBinary<T>(Stream binaryStream)
    {
      using (binaryStream)
        return (T) new BinaryFormatter().Deserialize(binaryStream);
    }

    private static void SerializeXML(Stream xmlStream, object objectToSerialize)
    {
      new XmlSerializer(objectToSerialize.GetType()).Serialize(xmlStream, objectToSerialize);
    }

    private static T DeSerializeXML<T>(Stream xmlStream)
    {
      using (XmlTextReader xmlTextReader = new XmlTextReader(xmlStream))
      {
        xmlTextReader.Normalization = false;
        try
        {
          return (T) new XmlSerializer(typeof (T)).Deserialize((XmlReader) xmlTextReader);
        }
        catch
        {
          xmlStream.Position = 0L;
          using (StreamXmlSanitizer streamXmlSanitizer = new StreamXmlSanitizer(xmlStream))
          {
            using (StringReader stringReader = new StringReader(SerializationHelper.CleanInvalidXmlChars(streamXmlSanitizer.ReadToEnd())))
              return (T) new XmlSerializer(typeof (T)).Deserialize((TextReader) stringReader);
          }
        }
      }
    }

    public static T DeSerializeXMLString<T>(string xmlStringContent)
    {
      using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xmlStringContent)))
        return SerializationHelper.DeSerializeXML<T>((Stream) memoryStream);
    }

    public static string CleanInvalidXmlChars(string text)
    {
      string pattern = "[^\\x09\\x0A\\x0D\\x20-\\xD7FF\\xE000-\\xFFFD\\x10000-x10FFFF]";
      return Regex.Replace(text, pattern, "");
    }

    public static string GetXslTransformedHtml(string xmlString, string xslString)
    {
      XslCompiledTransform compiledTransform = new XslCompiledTransform();
      XmlReader input = (XmlReader) new XmlTextReader((TextReader) new StringReader(xmlString));
      XmlReader stylesheet = (XmlReader) new XmlTextReader((TextReader) new StringReader(xslString));
      StringBuilder sb = new StringBuilder();
      XmlWriter results = (XmlWriter) new XmlTextWriter((TextWriter) new StringWriter(sb));
      compiledTransform.Load(stylesheet, new XsltSettings(true, true), (XmlResolver) new XmlUrlResolver());
      compiledTransform.Transform(input, results);
      return ((object) sb).ToString();
    }

    public enum FileType
    {
      Binary,
      XML,
    }
  }
}
