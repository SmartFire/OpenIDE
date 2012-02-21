using System;
using System.Linq;
using NUnit.Framework;
using OpenIDE.Bootstrapping;
using OpenIDE.FileSystem;
using OpenIDE.Messaging;
using OpenIDE.Arguments;
using OpenIDE.EditorEngineIntegration;
using OpenIDE.CodeEngineIntegration;

namespace OpenIDE.Tests
{
	[TestFixture]
	public class DIContainerTests
	{
		[Test]
		public void Should_resolve_types()
		{
			var container = new DIContainer(
				new AppSettings(
					"",
					() => { return new ICommandHandler[] {}; },
					() => { return new ICommandHandler[] {}; }));
			
			Assert.That(container.ICommandHandlers().Count(), Is.EqualTo(15));
			
			Assert.That(container.IFS(), Is.InstanceOf<IFS>());
			Assert.That(container.IMessageBus(), Is.InstanceOf<IMessageBus>());
			Assert.That(container.ILocateEditorEngine(), Is.InstanceOf<ILocateEditorEngine>());
			Assert.That(container.ICodeEngineLocator(), Is.InstanceOf<ICodeEngineLocator>());
		}
	}
}

