using System.Collections;
using System.ComponentModel;
using ZLinq;

namespace ProjectAFS.Core.Utility.Enumerable;

public static class EnumExtension
{
	/// <summary>
	/// Fetches the description attribute value of an enum value.
	/// </summary>
	/// <param name="value">The enum value where the description is fetched from.</param>
	/// <param name="strict">If true, throws an exception when the description attribute is not found.</param>
	/// <typeparam name="T">Type of the enum.</typeparam>
	/// <returns>The description string if found; otherwise, the enum name/value as string or throws an exception if strict is <see langword="true"/>.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the description attribute is not found and strict is set to <see langword="true"/>.</exception>
	public static string GetDescription<T>(this T value, bool strict = false) where T : Enum
	{
		var type = value.GetType();
		var name = Enum.GetName(type, value);
		if (name != null)
		{
			var field = type.GetField(name);
			if (field != null)
			{
				if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
				{
					return attr.Description;
				}
			}
		}
		if (strict)
		{
			throw new InvalidOperationException($"Description attribute not found for enum value '{value}'.");
		}
		return name ?? value.ToString();
	}
	
	public static string GetDescription(this Enum value, bool strict = false)
	{
		var type = value.GetType();
		var name = Enum.GetName(type, value);
		if (name != null)
		{
			var field = type.GetField(name);
			if (field != null)
			{
				if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
				{
					return attr.Description;
				}
			}
		}
		if (strict)
		{
			throw new InvalidOperationException($"Description attribute not found for enum value '{value}'.");
		}
		return name ?? value.ToString();
	}
	
	public static T FindByDescription<T>(string description, bool strict = false) where T : Enum
	{
		foreach (var field in typeof(T).GetFields())
		{
			if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
			{
				if (attr.Description == description)
				{
					return (T)field.GetValue(null)!;
				}
			}
			else
			{
				if (field.Name == description)
				{
					return (T)field.GetValue(null)!;
				}
			}
		}
		if (strict)
		{
			throw new ArgumentException($"No enum value with description '{description}' found in enum '{typeof(T).Name}'.");
		}
		return default!;
	}
	
	public static object FindByDescription(Type enumType, string description, bool strict = false)
	{
		foreach (var field in enumType.GetFields())
		{
			if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
			{
				if (attr.Description == description)
				{
					return field.GetValue(null)!;
				}
			}
			else
			{
				if (field.Name == description)
				{
					return field.GetValue(null)!;
				}
			}
		}
		if (strict)
		{
			throw new ArgumentException($"No enum value with description '{description}' found in enum '{enumType.Name}'.");
		}
		return Activator.CreateInstance(enumType)!;
	}
}

public static class Enumerable
{
	public static IEnumerable<T> Create<T>(params T[] items)
	{
		foreach (var item in items)
		{
			yield return item;
		}
	}
}