using System;
using System.Collections;
using Faithlife.Build;
using LibGit2Sharp;

internal static class Build
{
	public static int Main(string[] args) => BuildRunner.Execute(args, build =>
	{
		build.AddDotNetTargets(
			new DotNetBuildSettings
			{
				DocsSettings = new DotNetDocsSettings
				{
					GitLogin = new GitLoginInfo("ejball", Environment.GetEnvironmentVariable("BUILD_BOT_PASSWORD") ?? ""),
					GitAuthor = new GitAuthorInfo("ejball", "ejball@gmail.com"),
					SourceCodeUrl = "https://github.com/ejball/RegexMatchValues/tree/master/src",
				},
			});

		build.Target("build")
			.Does(() =>
			{
				using (var repository = new Repository("."))
				{
					Console.WriteLine("HEAD: " + repository.Head.FriendlyName);
					foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
						Console.WriteLine(variable.Key + "=" + variable.Value);
				}
			});

		build.Target("default")
			.DependsOn("build");
	});
}
