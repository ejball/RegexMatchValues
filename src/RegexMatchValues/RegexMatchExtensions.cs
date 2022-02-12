using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RegexMatchValues;

/// <summary>
/// Extension methods for extracting values from regular expression matches.
/// </summary>
/// <remarks>
/// <para>These methods convert successful matches into values of the specified type.</para>
/// <para>For simple types, the text of the first capturing group is converted to that type; if there
/// are no capturing groups, the entire match is converted.</para>
/// <para>For tuple types, each capturing group is converted to an item in the requested tuple.</para>
/// <para>If a group is not successful, the <c>default</c> value is returned, which is <c>null</c>
/// for nullable types, but zero for non-nullable numeric types.</para>
/// <para>If a group has multiple captures and the target type is an array, each
/// capture is converted to an item in the returned array.</para>
/// <para>If the target type is <c>string</c>, the text of the group/capture is returned.</para>
/// <para>If the target type is <c>bool</c>, <c>true</c> is returned (unless the group
/// was not successful, per above).</para>
/// <para>If the target type is a numeric type or <c>Guid</c>, the text of the group/capture
/// is parsed into that type using the invariant culture and default settings, which allow leading and
/// trailing whitespace. If the text is empty or only whitespace and the type is nullable, null is returned.
/// If the text cannot be parsed into that type, the corresponding <see cref="FormatException"/>
/// is thrown.</para>
/// <para>If the target type is an enumerated type, the text of the group/capture is parsed as that type,
/// ignoring case.</para>
/// <para>If the target type is <c>Group</c> or <c>Capture</c>, the corresponding object
/// of that type for the group/capture is returned.</para>
/// <para>If an unsupported type is used, <see cref="InvalidOperationException"/> is thrown.</para>
/// </remarks>
public static class RegexMatchExtensions
{
	/// <summary>
	/// Returns a value of the specified type for the match.
	/// </summary>
	/// <typeparam name="T">The desired type. See <see cref="RegexMatchExtensions"/> for supported types.</typeparam>
	/// <param name="match">The match.</param>
	/// <returns>The corresponding value of the specified type.</returns>
	/// <exception cref="FormatException">The text of the capture cannot be parsed as the specified type.</exception>
	/// <exception cref="InvalidOperationException">The match failed, or the specified type is not supported.</exception>
	public static T Get<T>(this Match match) => match.Get<T>(Array.Empty<string>());

	/// <summary>
	/// Returns a value of the specified type for the match.
	/// </summary>
	/// <typeparam name="T">The desired type. See <see cref="RegexMatchExtensions"/> for supported types.</typeparam>
	/// <param name="match">The match.</param>
	/// <param name="groupNames">The group names to return.</param>
	/// <returns>The corresponding value of the specified type.</returns>
	/// <exception cref="FormatException">The text of the capture cannot be parsed as the specified type.</exception>
	/// <exception cref="InvalidOperationException">The match failed, or the specified type is not supported.</exception>
	public static T Get<T>(this Match match, params string[] groupNames) => match.TryGet<T>(groupNames, out var value) ? value! : throw new InvalidOperationException("Match failed.");

	/// <summary>
	/// Attempts to return a value of the specified type for the match.
	/// </summary>
	/// <typeparam name="T">The desired type. See <see cref="RegexMatchExtensions"/> for supported types.</typeparam>
	/// <param name="match">The match.</param>
	/// <returns>The corresponding value if the match was successful; <c>default(T)</c> otherwise.</returns>
	/// <exception cref="FormatException">The text of the capture cannot be parsed as the specified type.</exception>
	/// <exception cref="InvalidOperationException">The specified type is not supported.</exception>
	public static T? TryGet<T>(this Match match) => match.TryGet<T>(Array.Empty<string>());

	/// <summary>
	/// Attempts to return a value of the specified type for the match.
	/// </summary>
	/// <typeparam name="T">The desired type. See <see cref="RegexMatchExtensions"/> for supported types.</typeparam>
	/// <param name="match">The match.</param>
	/// <param name="groupNames">The group names to return.</param>
	/// <returns>The corresponding value if the match was successful; <c>default(T)</c> otherwise.</returns>
	/// <exception cref="FormatException">The text of the capture cannot be parsed as the specified type.</exception>
	/// <exception cref="InvalidOperationException">The specified type is not supported.</exception>
	public static T? TryGet<T>(this Match match, params string[] groupNames) => match.TryGet<T>(groupNames, out var value) ? value : default;

	/// <summary>
	/// Attempts to return a value of the specified type for the match.
	/// </summary>
	/// <typeparam name="T">The desired type. See <see cref="RegexMatchExtensions"/> for supported types.</typeparam>
	/// <param name="match">The match.</param>
	/// <param name="value">The returned value.</param>
	/// <returns>True if the match was successful; false otherwise.</returns>
	/// <exception cref="FormatException">The text of the capture cannot be parsed as the specified type.</exception>
	/// <exception cref="InvalidOperationException">The specified type is not supported.</exception>
	public static bool TryGet<T>(this Match match, [MaybeNullWhen(returnValue: false)] out T value) => match.TryGet(Array.Empty<string>(), out value);

	/// <summary>
	/// Attempts to return a value of the specified type for the match.
	/// </summary>
	/// <typeparam name="T">The desired type. See <see cref="RegexMatchExtensions"/> for supported types.</typeparam>
	/// <param name="match">The match.</param>
	/// <param name="groupNames">The group names to return.</param>
	/// <param name="value">The returned value.</param>
	/// <returns>True if the match was successful; false otherwise.</returns>
	/// <exception cref="FormatException">The text of the capture cannot be parsed as the specified type.</exception>
	/// <exception cref="InvalidOperationException">The specified type is not supported.</exception>
	public static bool TryGet<T>(this Match match, string[] groupNames, [MaybeNullWhen(returnValue: false)] out T value)
	{
		if (match is null)
			throw new ArgumentNullException(nameof(match));

		if (!match.Success)
		{
			value = default!;
			return false;
		}

		var groupNameCount = (groupNames ?? throw new ArgumentNullException(nameof(groupNames))).Length;

		var type = typeof(T);
		if (TupleInfo.IsTupleType(type))
		{
			var tupleInfo = TupleInfo.GetInfo(type);
			var tupleTypes = tupleInfo.ItemTypes;
			var count = tupleTypes.Count;
			if (count < 2)
				throw new InvalidOperationException($"Tuple must have at least two types: {type.FullName}");

			var items = new object?[count];

			if (groupNameCount != 0)
			{
				if (groupNameCount != count)
					throw new InvalidOperationException($"There must be the same number of group names as tuple values ({count}).");

				for (var index = 0; index < count; index++)
					items[index] = ConvertGroup(match.Groups[groupNames[index]], tupleTypes[index]);
			}
			else
			{
				if (match.Groups.Count < count + 1)
					throw new InvalidOperationException($"Regex must have at least {count} capturing groups; it has {match.Groups.Count - 1}.");

				for (var index = 0; index < count; index++)
					items[index] = ConvertGroup(match.Groups[index + 1], tupleTypes[index]);
			}

			value = (T) tupleInfo.CreateNew(items);
		}
		else if (groupNameCount != 0)
		{
			if (groupNameCount != 1)
				throw new InvalidOperationException("There must be exactly one group name for the specified type.");

			value = (T) ConvertGroup(match.Groups[groupNames[0]], type)!;
		}
		else
		{
			value = (T) ConvertGroup(match.Groups.Count > 1 ? match.Groups[1] : match.Groups[0], type)!;
		}

		return true;
	}

	private static object? ConvertGroup(Group group, Type type)
	{
		if (type == typeof(Group))
			return group;

		if (!group.Success)
			return null;

		if (type.IsArray)
			return ConvertCaptures(group.Captures, type.GetElementType()!);

		return ConvertCapture(group, type);
	}

	private static object ConvertCaptures(CaptureCollection captures, Type type)
	{
		var count = captures.Count;
		var array = Array.CreateInstance(type, count);
		for (var index = 0; index < count; index++)
			array.SetValue(ConvertCapture(captures[index], type), index);
		return array;
	}

	private static object? ConvertCapture(Capture capture, Type type)
	{
		if (type == typeof(Capture))
			return capture;

		if (type == typeof(string))
			return capture.Value;

		bool isNullable;
		var underlyingType = Nullable.GetUnderlyingType(type);
		if (underlyingType != null)
		{
			isNullable = true;
			type = underlyingType;
		}
		else
		{
			isNullable = !type.IsValueType;
		}

		if (type == typeof(bool))
			return true;

#if NET6_0_OR_GREATER
		var value = capture.ValueSpan;
		if (isNullable && value.Trim().Length == 0)
			return null;
#else
		var value = capture.Value;
		if (isNullable && string.IsNullOrWhiteSpace(value))
			return null;
#endif

		if (type == typeof(int))
			return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
		if (type == typeof(long))
			return long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
		if (type == typeof(short))
			return short.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
		if (type == typeof(byte))
			return byte.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
		if (type == typeof(double))
			return double.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
		if (type == typeof(float))
			return float.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
		if (type == typeof(decimal))
			return decimal.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
		if (type == typeof(Guid))
			return Guid.Parse(value);
		if (type == typeof(uint))
			return uint.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
		if (type == typeof(ulong))
			return ulong.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
		if (type == typeof(ushort))
			return ushort.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
		if (type == typeof(sbyte))
			return sbyte.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

		if (type.IsEnum)
			return Enum.Parse(type, value, ignoreCase: true);

		throw new InvalidOperationException($"Type not supported: {type.FullName}");
	}
}
