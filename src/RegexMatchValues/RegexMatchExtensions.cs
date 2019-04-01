using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Faithlife.Reflection;

namespace RegexMatchValues
{
	/// <summary>
	/// Extension methods for extracting values from regular expression matches.
	/// </summary>
	public static class RegexMatchExtensions
	{
		/// <summary>
		/// Attempts to return a value of the specified type for the match.
		/// </summary>
		/// <typeparam name="T">The desired type.</typeparam>
		/// <param name="match">The match.</param>
		/// <param name="value">The returned value.</param>
		/// <returns>True if the match was successful; false otherwise.</returns>
		public static bool TryGet<T>(this Match match, out T value)
		{
			object result = null;

			if (match.Success)
			{
				var type = typeof(T);
				if (TupleInfo.IsTupleType(type))
				{
					var tupleInfo = TupleInfo.GetInfo(type);
					var tupleTypes = tupleInfo.ItemTypes;
					var count = tupleTypes.Count;
					if (count < 2)
						throw new InvalidOperationException($"Tuple must have at least two types: {type.FullName}");
					if (match.Groups.Count < count + 1)
						throw new InvalidOperationException($"Regex must have at least {count} capturing groups; it has {match.Groups.Count - 1}.");

					var items = new object[count];
					for (int index = 0; index < count; index++)
						items[index] = ConvertGroup(match.Groups[index + 1], tupleTypes[index]);
					result = tupleInfo.CreateNew(items);
				}
				else
				{
					result = ConvertGroup(match.Groups.Count > 1 ? match.Groups[1] : match.Groups[0], type);
				}
			}

			if (result == null)
			{
				value = default;
				return false;
			}
			else
			{
				value = (T) result;
				return true;
			}
		}

		/// <summary>
		/// Return a value of the specified type for the match.
		/// </summary>
		/// <typeparam name="T">The desired type.</typeparam>
		/// <param name="match">The match.</param>
		/// <returns>The corresponding value if the match was successful; <c>default(T)</c> otherwise.</returns>
		public static T Get<T>(this Match match) => match.TryGet(out T value) ? value : default;

		private static object ConvertGroup(Group group, Type type)
		{
			if (type == typeof(Group))
				return group;

			if (!group.Success)
				return null;

			if (type.IsArray)
				return ConvertCaptures(group.Captures, type.GetElementType());

			return ConvertCapture(group, type);
		}

		private static object ConvertCaptures(CaptureCollection captures, Type type)
		{
			int count = captures.Count;
			var array = Array.CreateInstance(type, count);
			for (int index = 0; index < count; index++)
				array.SetValue(ConvertCapture(captures[index], type), index);
			return array;
		}

		private static object ConvertCapture(Capture capture, Type type)
		{
			if (type == typeof(Capture))
				return capture;

			string value = capture.Value;
			if (type == typeof(string))
				return value;

			if (Nullable.GetUnderlyingType(type) is Type underlyingType)
				type = underlyingType;

			if (type == typeof(bool))
				return true;

			if (string.IsNullOrWhiteSpace(value))
				return null;

			if (s_parsers.Value.TryGetValue(type, out var parser))
				return parser(value, CultureInfo.InvariantCulture);

			if (type.IsEnum)
				return Enum.Parse(type, value, ignoreCase: true);

			throw new InvalidOperationException($"Type not supported: {type.FullName}");
		}

		private static readonly Lazy<IReadOnlyDictionary<Type, Func<string, CultureInfo, object>>> s_parsers =
			new Lazy<IReadOnlyDictionary<Type, Func<string, CultureInfo, object>>>(CreateParsers);

		private static IReadOnlyDictionary<Type, Func<string, CultureInfo, object>> CreateParsers()
		{
			var parsers = new Dictionary<Type, Func<string, CultureInfo, object>>();

			void addParser1<T>(Func<string, T> parser) => parsers.Add(typeof(T), (v, _) => parser(v));
			void addParser2<T>(Func<string, CultureInfo, T> parser) => parsers.Add(typeof(T), (v, c) => parser(v, c));

			addParser1(bool.Parse);
			addParser2(byte.Parse);
			addParser2(sbyte.Parse);
			addParser2(short.Parse);
			addParser2(ushort.Parse);
			addParser2(int.Parse);
			addParser2(uint.Parse);
			addParser2(long.Parse);
			addParser2(ulong.Parse);
			addParser2(float.Parse);
			addParser2(double.Parse);
			addParser2(decimal.Parse);
			addParser1(Guid.Parse);

			return parsers;
		}
	}
}
