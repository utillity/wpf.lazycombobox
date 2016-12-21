using System;
using System.ComponentModel;
using System.Linq;

namespace uTILLIty.WPF.Demo
{
	public class EnumItem
	{
		public EnumItem(object id, string displayName)
		{
			Id = id;
			DisplayName = displayName;
		}

		public object Id { get; }
		public string DisplayName { get; }

		public static EnumItem[] BuildFromEnumeration(Type enumType)
		{
			if (!enumType.IsEnum)
				throw new InvalidOperationException();
			var values =
				Enum.GetValues(enumType)
					.Cast<object>()
					.Select(v => new EnumItem(v, GetEnumEntryDisplayName(enumType, v)))
					.OrderBy(i => i.DisplayName)
					.ToArray();

			return values;
		}

		private static string GetEnumEntryDisplayName(Type enumType, object value)
		{
			var fi = enumType.GetField(value.ToString());

			var attrs = Attribute.GetCustomAttributes(fi, true).OfType<DescriptionAttribute>();
			return attrs.Select(a => a.Description).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d)) ?? value.ToString();
		}
	}
}