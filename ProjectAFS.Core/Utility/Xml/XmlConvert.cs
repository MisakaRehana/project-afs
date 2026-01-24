using System.Xml;
using System.Xml.Serialization;

namespace ProjectAFS.Core.Utility.Xml;

/// <summary>
/// Provides methods for serializing and deserializing objects to and from XML format.
/// </summary>
public static class XmlConvert
{
	public static object? DeserializeObject(string xml, Type type)
	{
		var serializer = new XmlSerializer(type);
		using var reader = new StringReader(xml);
		return serializer.Deserialize(reader);
	}
	
	public static T DeserializeObject<T>(string xml)
	{
		var serializer = new XmlSerializer(typeof(T));
		using var reader = new StringReader(xml);
		return (T)serializer.Deserialize(reader)!;
	}
	
	public static string SerializeObject(object obj)
	{
		var namespaces = new XmlSerializerNamespaces();
		namespaces.Add(string.Empty, string.Empty); // Remove xmlns:xsi and xmlns:xsd declarations
		var settings = new XmlWriterSettings()
		{
			Indent = true,
			OmitXmlDeclaration = true
		};
		
		var serializer = new XmlSerializer(obj.GetType());
		using var writer = new StringWriter();
		using var xmlWriter = XmlWriter.Create(writer, settings);
		serializer.Serialize(xmlWriter, obj, namespaces);
		return writer.ToString();
	}
	
	public static string SerializeObject<T>(T obj)
	{
		var namespaces = new XmlSerializerNamespaces();
		namespaces.Add(string.Empty, string.Empty); // Remove xmlns:xsi and xmlns:xsd declarations
		var settings = new XmlWriterSettings()
		{
			Indent = true,
			OmitXmlDeclaration = true
		};
		
		var serializer = new XmlSerializer(typeof(T));
		using var writer = new StringWriter();
		using var xmlWriter = XmlWriter.Create(writer, settings);
		serializer.Serialize(xmlWriter, obj, namespaces);
		return writer.ToString();
	}
}