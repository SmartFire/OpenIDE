using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using OpenIDE;
using OpenIDE.Messaging;
using OpenIDE.Core.FileSystem;
using OpenIDE.Bootstrapping;
using OpenIDE.Arguments;
using OpenIDE.Core.Logging;
using OpenIDE.Core.Language;
using OpenIDE.CommandBuilding;
using OpenIDE.Core.CommandBuilding;
using OpenIDE.Core.Profiles;
using OpenIDE.Core.Commands;
using OpenIDE.Core.Definitions;

namespace oi
{
	class MainClass
	{
		private const string PROFILE = "--profile=";
		private const string GLOBAL_PROFILE = "--global-profile=";
		private const string FORCE_SKIP_DEFINITION_REBUILD = "--skip-definition-rebuild";

		private static bool _rebuildDefinitions = true;

		public static void Main(string[] args)
		{
			if (args.Any(x => x == "--logging"))
				Logger.Assign(new ConsoleLogger());

			Logger.Write("Parsing profiles");
			args = parseProfile(args);
			Logger.Write("Initializing application");
			Bootstrapper.Initialize();			
			
			Logger.Write("Getting definition builder");
			var builder = Bootstrapper.GetDefinitionBuilder(); 
			Logger.Write("Building definitions");
			builder.Build(_rebuildDefinitions);
			
			Logger.Write("Parsing arguments");
			args = Bootstrapper.Settings.Parse(args);

			if (args.Length == 0) {
				printUsage(null);
				Logger.Write("Application exiting");
				return;
			}

			var arguments = new List<string>();
			arguments.AddRange(args);
			Logger.Write("Getting command from arguments");
			var cmd = builder.Get(arguments.ToArray());
			if (cmd == null) {
				printUsage(arguments[0]);
				Logger.Write("Application exiting");
				return;
			}
			Logger.Write("Running command {0} of type {1}", cmd.Name, cmd.Type);
			if (!new CommandRunner(Bootstrapper.DispatchEvent).Run(cmd, arguments))
				printUsage(cmd.Name);

			Logger.Write("Waiting for background commands");
			while (Bootstrapper.IsProcessing)
				Thread.Sleep(10);
			Logger.Write("Application exiting");
		}

		private static string[] parseProfile(string[] args)
		{
			var newArgs = new List<string>();
			foreach (var arg in args) {
				if (arg.StartsWith(PROFILE)) {
					ProfileLocator.ActiveLocalProfile =
						arg.Substring(PROFILE.Length, arg.Length - PROFILE.Length);
				} else if (arg.StartsWith(GLOBAL_PROFILE)) {
					ProfileLocator.ActiveGlobalProfile =
						arg.Substring(GLOBAL_PROFILE.Length, arg.Length - GLOBAL_PROFILE.Length);
				} else if (arg == FORCE_SKIP_DEFINITION_REBUILD) {
					_rebuildDefinitions = false;
				} else {
					newArgs.Add(arg);
				}
			}
			return newArgs.ToArray();
		}

		private static void printUsage(string commandName)
		{
			var definitions = Bootstrapper.GetDefinitionBuilder().Definitions;
			if (commandName == null) {
				Console.WriteLine("OpenIDE v0.2");
				Console.WriteLine("OpenIDE is a scriptable environment that provides simple IDE features around your favorite text exitor.");
				Console.WriteLine("(http://www.openide.net, http://github.com/ContinuousTests/OpenIDE)");
				Console.WriteLine();
			}
			if (commandName != null) {
				definitions = definitions 
					.Where(x => 
						 x.Name.Contains(commandName) ||
						(
							x.Parameters.Any(y => y.Required && matchName(y.Name, commandName))
						));
				if (definitions.Count() > 0)
					Console.WriteLine("Did you mean:");
			}
			if (definitions.Count() > 0 && commandName == null) {
				Console.WriteLine();
				Console.WriteLine("\t[{0}=NAME] : Force command to run under specified profile", PROFILE);
				Console.WriteLine("\t[{0}=NAME] : Force command to run under specified global profile", GLOBAL_PROFILE);
				Console.WriteLine("\t[--default.language=NAME] : Force command to run using specified default language");
				Console.WriteLine("\t[--enabled.languages=LANGUAGE_LIST] : Force command to run using specified languages");
				Console.WriteLine("\t[--logging] : Enables logging to console");
				Console.WriteLine("\t[--raw] : Prints raw output");
				Console.WriteLine("\t[--skip-definition-rebuild] : Forces the system not to rebuild command definitions");
				Console.WriteLine();
			}
			definitions
				.OrderBy(x => x.Name)
				.ToList()
				.ForEach(x => UsagePrinter.PrintDefinition(x));
		}

		private static bool matchName(string actual, string parameter)
		{
			if (actual.Contains(parameter))
				return true;
			if (Math.Abs(actual.Length - parameter.Length) > 2)
				return false;
			var containedCharacters = 0;
			for (int i = 0; i < actual.Length; i++) {
				if (parameter.Contains(actual[i]))
					containedCharacters++;
			}
			if (containedCharacters > actual.Length - 2)
				return true;
			return false;
		}
	}
}

