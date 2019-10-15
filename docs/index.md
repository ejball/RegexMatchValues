# RegexMatchValues

**RegexMatchValues** converts regular expression matches to strong types.

[![NuGet](https://img.shields.io/nuget/v/RegexMatchValues.svg)](https://www.nuget.org/packages/RegexMatchValues)

## Usage

The [RegexMatchExtensions](RegexMatchValues/RegexMatchExtensions.md) static class provides a few simple extension methods on [`Match`](https://docs.microsoft.com/dotnet/api/system.text.regularexpressions.match) that convert the capturing groups of the regular expression into a tuple. [(Try it!)](https://dotnetfiddle.net/e7dSIl)

```csharp
var text = "Use 22/7 for pi.";
var (numerator, denominator) =
    Regex.Match(text, @"(\d+)/(\d+)").Get<(int, int)>();
Console.WriteLine((double) numerator / denominator);
// output: 3.142857142857143
```

[`Get<T>()`](RegexMatchValues/RegexMatchExtensions/Get.md) throws `InvalidOperationException` if the match fails, so use one of the [`TryGet<T>()`](RegexMatchValues/RegexMatchExtensions/TryGet.md) overloads if that is a possibility. [(Try it!)](https://dotnetfiddle.net/t2vtM0)

```csharp
var text = "Use 3.14 for pi.";
if (Regex.Match(text, @"(\d+)/(\d+)").TryGet(out (int N, int D) fraction))
    Console.WriteLine((double) fraction.N / fraction.D);
else
    Console.WriteLine("No fraction found.");
```

Other types besides tuples are also supported. See the [remarks](RegexMatchValues/RegexMatchExtensions.md) in the documentation for the full details.
