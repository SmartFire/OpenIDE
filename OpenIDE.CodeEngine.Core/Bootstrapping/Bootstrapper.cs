using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using CoreExtensions;
using OpenIDE.CodeEngine.Core.Caching;
using OpenIDE.CodeEngine.Core.ChangeTrackers;
using OpenIDE.CodeEngine.Core.EditorEngine;
using OpenIDE.CodeEngine.Core.Endpoints;
using OpenIDE.CodeEngine.Core.Endpoints.Tcp;
using OpenIDE.CodeEngine.Core.Handlers;
using OpenIDE.Core.Caching;
using OpenIDE.Core.Commands;
using OpenIDE.Core.Config;
using OpenIDE.Core.Configs;
using OpenIDE.Core.Integration;
using OpenIDE.Core.Language;
using OpenIDE.Core.Logging;
using OpenIDE.Core.Profiles;
using OpenIDE.Core.Windowing;


namespace OpenIDE.CodeEngine.Core.Bootstrapping
{
	public class Bootstrapper
	{
		private static string _path;
		private static PluginFileTracker _tracker;
		private static List<IHandler> _handlers = new List<IHandler>();
		private static CommandEndpoint _endpoint;
		private static EventEndpoint _eventEndpoint;
		private static OutputEndpoint _outputEndpoint;
		private static TypeCache _cache;
        private static PluginLocator _pluginLocator;
		private static Interpreters _interpreters;

		public static CommandEndpoint GetEndpoint(string path, string[] enabledLanguages)
		{
			_path = path;
			var reader = new ConfigReader(_path);
			_interpreters = new Interpreters(_path);
			ProcessExtensions.GetInterpreter = 
				(file) => {
						var interpreters = _interpreters
							.GetInterpreterFor(Path.GetExtension(file));
						return interpreters;
					};
            _cache = new TypeCache();
            _outputEndpoint = new OutputEndpoint(_path);
			Logger.Write("Event endpoint serving on port: {0}", _outputEndpoint.Port);
            var responseDispatcher = new ResponseDispatcher(
            	_path,
            	false,
            	"language-output ",
            	(p, m) => _outputEndpoint.Send(p, m),
            	(m) => _endpoint.Handle(m),
            	(m) => {}
            );
            responseDispatcher.OnlyCommands();
			_pluginLocator = new PluginLocator(
				enabledLanguages,
				new ProfileLocator(_path),
				(msg) => {
					responseDispatcher.Handle(false, msg);
				}
			);
			initPlugins(_pluginLocator);

			_eventEndpoint = new EventEndpoint(_path, _pluginLocator, _outputEndpoint);
			_eventEndpoint.Start();
			Logger.Write("Event endpoint listening on port: {0}", _eventEndpoint.Port);

			Logger.Write("Creating plugin file tracker");
			_tracker = new PluginFileTracker();
			Logger.Write("Starting plugin file tracker");
			var ignoreDirSetting = reader.Get("oi.ignore.directories");
			var ignoreDirectories = new string[] {};
			if (ignoreDirSetting != null) {
				ignoreDirectories = ignoreDirSetting
					.Split(new[] {','})
					.Select(x => {
						if (Path.IsPathRooted(x)) {
							return x;
						}
						return Path.Combine(_path, x);
					})
					.ToArray();
			}
			_tracker.Start(
				_path,
				_cache,
				_cache,
				_pluginLocator,
				_eventEndpoint,
				ignoreDirectories);
			Logger.Write("Plugin file tracker started");

            _endpoint = new CommandEndpoint(_path, _cache, _eventEndpoint);
			_endpoint.AddHandler(messageHandler);
			
			_handlers.AddRange(new IHandler[] {
					new GetProjectsHandler(_endpoint, _cache),
					new GetFilesHandler(_endpoint, _cache),
					new GetCodeRefsHandler(_endpoint, _cache),
					new GetSignatureRefsHandler(_endpoint, _cache),
					new GoToDefinitionHandler(_endpoint, _cache, _pluginLocator),
					new FindTypeHandler(_endpoint, _cache),
					new SnippetEditHandler(_endpoint, _cache, _path),
					new SnippetDeleteHandler(_cache, _path),
					new GetRScriptStateHandler(_endpoint, _eventEndpoint),
					new CompleteSnippetHandler(_cache, _path, _endpoint),
					new WriteOutputHandler(_eventEndpoint),
					new GetTokenPathHandler(_endpoint),

                    // Make sure this handler is the last one since the command can be file extension or language name
                    new LanguageCommandHandler(_endpoint, _cache, _pluginLocator)
				});
			Logger.Write("Command endpoint started");
			return _endpoint;
		}

		public static ICacheBuilder GetCacheBuilder()
		{
			return _cache;
		}

		public static void Shutdown()
		{
            shutdownPlugins(_pluginLocator);
			_tracker.Dispose();
			_endpoint.Stop();
			_eventEndpoint.Stop();
			_outputEndpoint.Stop();
		}

		private static void messageHandler(MessageArgs message, ITypeCache cache, Editor editor)
		{
			var msg = CommandMessage.New(message.Message);
			_handlers
				.Where(x => x.Handles(msg)).ToList()
				.ForEach(x => x.Handle(message.ClientID, msg));
		}

        private static void shutdownPlugins(PluginLocator locator)
		{
			try {
				var plugins = locator.Locate();
				foreach (var plugin in plugins) {
					try {
						Logger.Write("Shutting down plugin " + plugin.GetLanguage());
	                    plugin.Shutdown();
					} catch (Exception ex) {
						Logger.Write(ex.ToString());
					}
				}
			} catch {
			}
		}
		
		private static void initPlugins(PluginLocator locator)
		{
			var plugins = locator.Locate();
			foreach (var plugin in plugins) {
				try {
                    plugin.Initialize(_path);
                    plugin.GetCrawlFileTypes();
                    ThreadPool.QueueUserWorkItem(
                    	(o) => {
                    		try {
                    			var currentPlugin = (LanguagePlugin)o;
								var handler = new CrawlHandler(_cache, (s) => Logger.Write(s));
                    			handler.SetLanguage(currentPlugin.GetLanguage());
								currentPlugin.Crawl(new string[] { _path }, (line) => handler.Handle(line));
							} catch (Exception ex) {
								Logger.Write(ex.ToString());
							}
						},
						plugin);
				} catch (Exception ex) {
					Logger.Write(ex.ToString());
				}
			}
			Logger.Write("Plugins initialized");
		}
	}
}
