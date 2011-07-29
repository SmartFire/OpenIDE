using System;
using OpenIDENet.CodeEngine.Core.Caching;
using System.Collections.Generic;
using System.Linq;
namespace OpenIDENet.CodeEngine.Core.Crawlers
{
	enum Location
	{
        Unknown,
		Root,
		Namespace,
		Class,
        Interface,
		Struct,
		Enum,
		Method
	}

    public class CSharpFileParser
    {
        private object _padLock = new object();
        private ICacheBuilder _builder;
        private string _file;
        private string _content;
        private Location _suggestedLocation = Location.Unknown;
        private Location _currentLocation;
        private Stack<Location> _locationHierarchy = new Stack<Location>();
        private CSharpCodeNavigator _navigator;

        private Namespace _currentNamespace = null;

        public CSharpFileParser(ICacheBuilder builder)
        {
            _builder = builder;
        }

        public void ParseFile(string file, Func<string> getContent)
        {
            lock (_padLock)
            {
                _builder.AddFile(file);
                _file = file;
                _content = getContent();
                _currentLocation = Location.Root;
                _currentNamespace = null;
                _navigator = new CSharpCodeNavigator(
                    _content.ToCharArray(),
                    () =>
                    {
                        _locationHierarchy.Push(_currentLocation);
                        _currentLocation = _suggestedLocation;
                        _suggestedLocation = Location.Unknown;
                    },
                    () =>
                    {
                        _currentLocation = _locationHierarchy.Pop();
                    });
                parse();
            }
        }

        private void parse()
        {
            Word word;
            while ((word = _navigator.GetWord()) != null)
            {
                if (_currentLocation == Location.Root)
                    whenInRoot(word);
                else if (_currentLocation == Location.Namespace)
                    whenInNamespace(word);
            }
        }

        private void whenInRoot(Word word)
        {
            if (word.Text == "namespace")
                handleNamespace(word);
        }

        private void whenInNamespace(Word word)
        {
            if (word.Text == "class")
                handleClass(word);
            if (word.Text == "interface")
                handleInterface(word);
            if (word.Text == "struct")
                handleStruct(word);
            if (word.Text == "enum")
                handleEnum(word);

        }

        private void handleNamespace(Word word)
        {
            suggestLocation(Location.Namespace);
            var signature = _navigator.CollectSignature();
            var ns = new Namespace(
                _file,
                signature.Text,
                signature.Offset,
                signature.Line,
                signature.Column);
            _builder.AddNamespace(ns);
            _currentNamespace = ns;
        }

        private void handleClass(Word word)
        {
            suggestLocation(Location.Class);
            var signature = _navigator.CollectSignature();
            var ns = "";
            if (_currentNamespace != null)
                ns = _currentNamespace.Name;
            _builder.AddClass(
                new Class(
                    _file,
                    ns,
                    getNameFromSignature(signature.Text),
                    signature.Offset,
                    signature.Line,
                    signature.Column));
        }

        private void handleInterface(Word word)
        {
            suggestLocation(Location.Interface);
            var signature = _navigator.CollectSignature();
            var ns = "";
            if (_currentNamespace != null)
                ns = _currentNamespace.Name;
            _builder.AddInterface(
                new Interface(
                    _file,
                    ns,
                    getNameFromSignature(signature.Text),
                    signature.Offset,
                    signature.Line,
                    signature.Column));
        }

        private void handleStruct(Word word)
        {
            suggestLocation(Location.Struct);
            var signature = _navigator.CollectSignature();
            var ns = "";
            if (_currentNamespace != null)
                ns = _currentNamespace.Name;
            _builder.AddStruct(
                new Struct(
                    _file,
                    ns,
                    signature.Text,
                    signature.Offset,
                    signature.Line,
                    signature.Column));
        }

        private void handleEnum(Word word)
        {
            suggestLocation(Location.Enum);
            var signature = _navigator.CollectSignature();
            var ns = "";
            if (_currentNamespace != null)
                ns = _currentNamespace.Name;
            _builder.AddEnum(
                new EnumType(
                    _file,
                    ns,
                    signature.Text,
                    signature.Offset,
                    signature.Line,
                    signature.Column));
        }

        private string getNameFromSignature(string signature)
        {
            if (signature.IndexOf(":") != -1)
                return signature.Substring(0, signature.IndexOf(":"));
            return signature;
        }

        private void suggestLocation(Location location)
        {
            _suggestedLocation = location;
        }
    }
}
