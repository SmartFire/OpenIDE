using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using OpenIDE.Core.Config;
using OpenIDE.Core.Logging;
using OpenIDE.Core.Scripts;
using OpenIDE.Core.Language;
using OpenIDE.Core.Profiles;

namespace OpenIDE.Core.RScripts
{
	public class ReactiveScriptReader
	{
		private string _keyPath;
		private string _localScriptsPathDefault;
		private string _localScriptsPath;
		private string _globalScriptsPathDefault;
		private string _globalScriptsPath;
		private List<ReactiveScript> _scripts = new List<ReactiveScript>();
		private Func<PluginLocator> _pluginLocator;
		private Action<string,string> _outputDispatcher;
		private Action<string> _dispatch;

		public ReactiveScriptReader(string path, Func<PluginLocator> locator, Action<string,string> outputDispatcher, Action<string> dispatch)
		{
			_keyPath = path;
			_outputDispatcher = outputDispatcher;
			_dispatch = dispatch;
			var profiles = new ProfileLocator(_keyPath);
			_localScriptsPathDefault = getPath(profiles.GetLocalProfilePath("default"));
			_localScriptsPath = getPath(profiles.GetLocalProfilePath(profiles.GetActiveLocalProfile()));
			_globalScriptsPathDefault = getPath(profiles.GetGlobalProfilePath("default"));
			_globalScriptsPath = getPath(profiles.GetGlobalProfilePath(profiles.GetActiveGlobalProfile()));
			_pluginLocator = locator;
		}

		public List<ReactiveScript> Read()
		{
			if (_scripts.Count == 0)
				readScripts();
			return _scripts;
		}

		public List<ReactiveScript> ReadNonLanguageScripts()
		{
			if (_scripts.Count == 0)
				Read();
			var paths = GetPathsNoLanguage();
			return
				_scripts
					.Where(x => paths.Any(y => x.File.StartsWith(y)))
					.ToList();
		}

		public List<ReactiveScript> ReadLanguageScripts()
		{
			if (_scripts.Count == 0)
				Read();
			var paths = getLanguagePaths();
			return
				_scripts
					.Where(x => paths.Any(y => x.File.StartsWith(y)))
					.ToList();
		}

		public ReactiveScript ReadScript(string path)
		{
			return ReadScript(path, false);
		}

		public ReactiveScript ReadScript(string path, bool dispatchErrors)
		{
            try {
			    var script = new ReactiveScript(path, _keyPath, _outputDispatcher, _dispatch, dispatchErrors);
			    if (script.IsFaulted)
			    	return null;
			    return script;
            } catch (Exception ex) {
                Logger.Write(ex);
            }
            return null;
		}
		
		private void readScripts(string path)
		{
			if (path == null)
				return;
			if (!Directory.Exists(path))
				return;
			_scripts.AddRange(
				new ScriptFilter().GetScripts(path)
					.Select(x => ReadScript(x))
					.Where(x => x != null && !_scripts.Any(y => x.Name == y.Name)));
		}

		private void readScripts()
		{
			foreach (var path in GetPaths()) {
				Logger.Write("Reading scripts from: " + path);
				readScripts(path);
			}
		}

		public List<string> GetPaths()
		{
			var paths = new List<string>();
			addToList(paths, getLocal());
			addToList(paths, _localScriptsPathDefault);
			foreach (var path in getLanguagePaths())
				addToList(paths, path);
			addToList(paths, getGlobal());
			addToList(paths, _globalScriptsPathDefault);
			return paths;
		}

		public List<string> GetPathsNoLanguage()
		{
			var paths = new List<string>();
			addToList(paths, getLocal());
			addToList(paths, _localScriptsPathDefault);
			addToList(paths, getGlobal());
			addToList(paths, _globalScriptsPathDefault);
			return paths;
		}

		private List<string> getLanguagePaths() {
			var orderedProfilePaths = new List<string>();
			var profiles = new ProfileLocator(_keyPath);
			var profilePath = profiles.GetGlobalProfilePath("default");
			if (profilePath != null)
				orderedProfilePaths.Add(Path.Combine(profilePath, "languages"));
			profilePath = profiles.GetGlobalProfilePath(profiles.GetActiveGlobalProfile());
			if (profilePath != null)
				orderedProfilePaths.Add(Path.Combine(profilePath, "languages"));
			profilePath = profiles.GetLocalProfilePath("default");
			if (profilePath != null)
				orderedProfilePaths.Add(Path.Combine(profilePath, "languages"));
			profilePath = profiles.GetLocalProfilePath(profiles.GetActiveLocalProfile());
			if (profilePath != null)
				orderedProfilePaths.Add(Path.Combine(profilePath, "languages"));

			var paths = new List<string>();
			foreach (var plugin in _pluginLocator().Locate())
				addLanguagePath(plugin, orderedProfilePaths, ref paths);
			paths.Reverse();
			return paths;
		}

		private void addLanguagePath(LanguagePlugin plugin, List<string> rootPaths, ref List<string> paths) {
			var pluginRoot = Path.GetDirectoryName(plugin.FullPath);
			var index = rootPaths.IndexOf(pluginRoot);
			foreach (var root in rootPaths.Skip(index)) {
				var path =
						Path.Combine(
							root,
							Path.Combine(
								plugin.GetLanguage() + "-files",
								"rscripts"));
				if (!Directory.Exists(path))
					continue;
				paths.Add(path);
			}
		}

		private void addToList(List<string> list, string item)
		{
			if (item == null || item.Length == 0)
				return;
			if (!list.Contains(item))
				list.Add(item);
		}
		
		private string getGlobal()
		{
			return _globalScriptsPath;
		}

		private string getLocal()
		{
			return _localScriptsPath;
		}

		private string getPath(string path)
		{
			if (path == null)
				return null;
			return Path.Combine(path, "rscripts");
		}
	}
}
