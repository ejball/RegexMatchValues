using System;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace RegexMatchValues.Tests
{
	[TestFixture]
	public class RegexMatchExtensionsTests
	{
		[Test]
		public void StringFailedMatch()
		{
			Regex.Match("expressions", "c+").TryGet<string>().Should().BeNull();
			Invoking(() => Regex.Match("expressions", "c+").Get<string>()).Should().Throw<InvalidOperationException>();
		}

		[Test]
		public void StringNoGroupMatch()
		{
			Regex.Match("expressions", "s+").Get<string>().Should().Be("ss");
		}

		[Test]
		public void StringOneGroupMatch()
		{
			Regex.Match("expressions", "s+([aeiou]+)").Get<string>().Should().Be("io");
		}

		[Test]
		public void StringNamedGroupMatch()
		{
			Regex.Match("expressions", "s+(?'val'[aeiou]+)").Get<string>("val").Should().Be("io");
		}

		[Test]
		public void StringWrongNameGroupMatch()
		{
			Regex.Match("expressions", "s+(?'val'[aeiou]+)").Get<string>("xyzzy").Should().BeNull();
		}

		[Test]
		public void StringEmptyMatch()
		{
			Regex.Match("hello there", @"l()l").Get<string>().Should().Be("");
		}

		[Test]
		public void StringWhitespaceMatch()
		{
			Regex.Match("hello there", @"\s+").Get<string>().Should().Be(" ");
		}

		[Test]
		public void IntegerFailedMatch()
		{
			var text = "number";
			s_signedIntegerRegex.Match(text).TryGet<int>().Should().Be(0);
			s_signedIntegerRegex.Match(text).TryGet<int?>().Should().BeNull();
			s_signedIntegerRegex.Match(text).TryGet<long>().Should().Be(0);
			s_signedIntegerRegex.Match(text).TryGet<long?>().Should().BeNull();
			s_unsignedIntegerRegex.Match(text).TryGet<uint>().Should().Be(0);
			s_unsignedIntegerRegex.Match(text).TryGet<uint?>().Should().BeNull();
			s_unsignedIntegerRegex.Match(text).TryGet<ulong>().Should().Be(0);
			s_unsignedIntegerRegex.Match(text).TryGet<ulong?>().Should().BeNull();
			Invoking(() => s_signedIntegerRegex.Match(text).Get<int>()).Should().Throw<InvalidOperationException>();
			Invoking(() => s_signedIntegerRegex.Match(text).Get<int?>()).Should().Throw<InvalidOperationException>();
			Invoking(() => s_signedIntegerRegex.Match(text).Get<long>()).Should().Throw<InvalidOperationException>();
			Invoking(() => s_signedIntegerRegex.Match(text).Get<long?>()).Should().Throw<InvalidOperationException>();
			Invoking(() => s_unsignedIntegerRegex.Match(text).Get<uint>()).Should().Throw<InvalidOperationException>();
			Invoking(() => s_unsignedIntegerRegex.Match(text).Get<uint?>()).Should().Throw<InvalidOperationException>();
			Invoking(() => s_unsignedIntegerRegex.Match(text).Get<ulong>()).Should().Throw<InvalidOperationException>();
			Invoking(() => s_unsignedIntegerRegex.Match(text).Get<ulong?>()).Should().Throw<InvalidOperationException>();
		}

		[Test]
		public void IntegerNoGroupMatch()
		{
			var text = "number: -123";
			s_signedIntegerRegex.Match(text).Get<int>().Should().Be(-123);
			s_signedIntegerRegex.Match(text).Get<int?>().Should().Be(-123);
			s_signedIntegerRegex.Match(text).Get<long>().Should().Be(-123);
			s_signedIntegerRegex.Match(text).Get<long?>().Should().Be(-123);
			s_unsignedIntegerRegex.Match(text).Get<uint>().Should().Be(123);
			s_unsignedIntegerRegex.Match(text).Get<uint?>().Should().Be(123);
			s_unsignedIntegerRegex.Match(text).Get<ulong>().Should().Be(123);
			s_unsignedIntegerRegex.Match(text).Get<ulong?>().Should().Be(123);
		}

		[Test]
		public void IntegerOverflowException()
		{
			var text = "number: -123";
			Invoking(() => s_signedIntegerRegex.Match(text).Get<ulong>()).Should().Throw<OverflowException>();
		}

		[Test]
		public void IntegerFormatException()
		{
			Invoking(() => Regex.Match("x 0xDEAD x", @"x(.*)x").Get<int?>()).Should().Throw<FormatException>();
		}

		[Test]
		public void IntegerEmpty()
		{
			Invoking(() => Regex.Match("xx", @"x(.*)x").Get<int>()).Should().Throw<FormatException>();
			Regex.Match("xx", @"x(.*)x").Get<int?>().Should().Be(null);
			Invoking(() => Regex.Match("xxx", @"x(.*)x(.*)x").Get<(int, int)>()).Should().Throw<FormatException>();
			Regex.Match("xxx", @"x(.*)x(.*)x").Get<(int?, int?)>().Should().Be((default, default));
		}

		[Test]
		public void IntegerOnlyWhitespace()
		{
			Invoking(() => Regex.Match("x x", @"x(.*)x").Get<int>()).Should().Throw<FormatException>();
			Regex.Match("x x", @"x(.*)x").Get<int?>().Should().Be(null);
			Invoking(() => Regex.Match("x x x", @"x(.*)x(.*)x").Get<(int, int)>()).Should().Throw<FormatException>();
			Regex.Match("x x x", @"x(.*)x(.*)x").Get<(int?, int?)>().Should().Be((default, default));
		}

		[Test]
		public void IntegerWhitespaceTrimmed()
		{
			Regex.Match("x 42 x", @"x(.*)x").Get<int>().Should().Be(42);
			Regex.Match("x 42 x", @"x(.*)x").Get<int?>().Should().Be(42);
		}

		[Test]
		public void EnumParse()
		{
			Regex.Match("x righttoleft x", @"x(.*)x").Get<RegexOptions?>().Should().Be(RegexOptions.RightToLeft);
		}

		[Test]
		public void ThreeTupleMatch()
		{
			var match = Regex.Match("on 22 March 2019", @"([0-9]+)\s+([A-Z][a-z]+)\s+([0-9]+)");
			match.Get<(int, string, long)>().Should().Be((22, "March", 2019L));
			match.Get<(int, string, long)?>().Should().Be((22, "March", 2019L));
		}

		[Test]
		public void NamedThreeTupleMatch()
		{
			var match = Regex.Match("on 22 March 2019", @"(?'day'[0-9]+)\s+(?'month'[A-Z][a-z]+)\s+(?'year'[0-9]+)");
			match.Get<(string, int, long)>("month", "day", "year").Should().Be(("March", 22, 2019L));
			match.Get<(string, int, long)?>("month", "day", "year").Should().Be(("March", 22, 2019L));
		}

		[Test]
		public void TupleNoMatch()
		{
			var match = Regex.Match("nope", "(a) (b)");
			match.TryGet<(string?, int)>().Should().Be((null, 0));
			match.TryGet<(string?, int?)>().Should().Be((null, null));
			match.TryGet<(string?, int)?>().Should().BeNull();
			Invoking(() => match.Get<(string?, int)>()).Should().Throw<InvalidOperationException>();
			Invoking(() => match.Get<(string?, int?)>()).Should().Throw<InvalidOperationException>();
			Invoking(() => match.Get<(string?, int)?>()).Should().Throw<InvalidOperationException>();
		}

		[Test]
		public void TypeNotSupported()
		{
			Invoking(() => Regex.Match("type", "t+").Get<Type>()).Should().Throw<InvalidOperationException>();
		}

		[Test]
		public void SmallTuplesNotSupported()
		{
			Invoking(() => Regex.Match("type", "t+").Get<ValueTuple>()).Should().Throw<InvalidOperationException>();
			Invoking(() => Regex.Match("type", "t+").Get<ValueTuple<string>>()).Should().Throw<InvalidOperationException>();
		}

		[Test]
		public void NotEnoughGroups()
		{
			Invoking(() => Regex.Match("type", "t+").Get<(string, string)>()).Should().Throw<InvalidOperationException>();
			Invoking(() => Regex.Match("type", "(t+)").Get<(string, string)>()).Should().Throw<InvalidOperationException>();
		}

		[Test]
		public void WrongGroupNameCount()
		{
			Invoking(() => Regex.Match("type", "(?'x't+)").Get<string>("x", "y")).Should().Throw<InvalidOperationException>();
			Invoking(() => Regex.Match("type", "(?'x't+)").Get<(string, string)>("x")).Should().Throw<InvalidOperationException>();
			Invoking(() => Regex.Match("type", "(?'x't+)").Get<(string, string)>("x", "x", "x")).Should().Throw<InvalidOperationException>();
		}

		[Test]
		public void TooManyGroups()
		{
			Regex.Match("1 2 3", @"(\d) (\d) (\d)").Get<string>().Should().Be("1");
			Regex.Match("1 2 3", @"(\d) (\d) (\d)").Get<(string, string)>().Should().Be(("1", "2"));
		}

		[Test]
		public void NonCapturingGroups()
		{
			Regex.Match("1 2 3", @"(?:\d) (\d) (\d)").Get<string>().Should().Be("2");
			Regex.Match("1 2 3", @"(\d) (?:\d) (\d)").Get<(string, string)>().Should().Be(("1", "3"));
		}

		[Test]
		public void GetGroup()
		{
			Regex.Match("1 2 3", @"(\d) (\d) (\d)").Get<Group>()!.Value.Should().Be("1");
			Regex.Match("1 2 3", @"(\d) (\d) (\d)").Get<(string, Group)>().Item2.Value.Should().Be("2");
		}

		[Test]
		public void OptionalGroups()
		{
			Regex.Match("ac", @"(a)(b)?(c)").Get<(string, string?, string)>().Should().Be(("a", null, "c"));
			Regex.Match("ac", @"(a)(b?)(c)").Get<(string, string, string)>().Should().Be(("a", "", "c"));
		}

		[Test]
		public void BooleanValues()
		{
			Regex.Match("ac", @"(a)(b)?(c)").Get<(string, bool, bool)>().Should().Be(("a", false, true));
			Regex.Match("ac", @"(a)(b?)(c)").Get<(string, bool, bool)>().Should().Be(("a", true, true));
		}

		[Test]
		public void Matches()
		{
			Regex.Matches("867-5309", "[0-9]+").Select(x => x.Get<int>()).Should().Equal(867, 5309);
		}

		[Test]
		public void AddInPlace()
		{
			static string AddPairs(Match match)
			{
				var (first, second) = match.Get<(int, int)>();
				return $"{first + second}";
			}

			Regex.Replace("1+2 3+4 5+6", @"([0-9]+)\+([0-9]+)", AddPairs).Should().Be("3 7 11");
		}

		[Test]
		public void LastCapture()
		{
			Regex.Match("find 1 2 3 5 8", @"(([0-9]+)\s*)+").Get<(bool, int)>().Should().Be((true, 8));
		}

		[Test]
		public void CapturesAsIntegers()
		{
			var (match, numbers) = Regex.Match("find 1 2 3 5 8", @"(([0-9]+)\s*)+").Get<(bool, int[])>();
			match.Should().BeTrue();
			numbers.Should().Equal(1, 2, 3, 5, 8);
		}

		[Test]
		public void CapturesAsCaptures()
		{
			Regex.Match("find 1 2 3 5 8", @"(?:([0-9]+)\s*)+").Get<Capture[]>().Select(x => x.Value).Should().Equal("1", "2", "3", "5", "8");
		}

		private static readonly Regex s_signedIntegerRegex = new Regex(@"[-0-9]+");
		private static readonly Regex s_unsignedIntegerRegex = new Regex(@"[0-9]+");
	}
}
