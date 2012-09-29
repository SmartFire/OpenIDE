﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using OpenIDE.Core.Caching;
using OpenIDE.CodeEngine.Core.Caching;
using OpenIDE.CodeEngine.Core.Caching.Search;
using System.IO;

namespace OpenIDE.CodeEngine.Core.Tests.Caching
{
    [TestFixture]
    public class TypeCacheTests
    {
        [Test]
        public void When_searching_for_a_file_it_will_return()
        {
            var cache = new TypeCache();
            cache.Add(toFile("/Some/Path/File1.cs").SetFileSearch());
            cache.Add(toFile("/Some/Path/File2.cs").SetFileSearch());

            var verifier = new ResultVerifier(cache.FindFiles("File1"));
            verifier.VerifyCount(1);
            verifier.Verify(0, FileFindResultType.File, to("/Some/Path/File1.cs"));
        }

        [Test]
        public void When_searching_for_a_file_it_will_return_the_files_with_the_lowest_hierarchical_level()
        {
            var cache = new TypeCache();
            cache.Add(toFile("/Some/Path/File1.cs").SetFileSearch());
            cache.Add(toFile("/Some/Path/File2.cs").SetFileSearch());
            cache.Add(toFile("/Some/Path/In/AnotherpathPlace/File2.cs").SetFileSearch());

            var verifier = new ResultVerifier(cache.FindFiles("Path"));
            verifier.VerifyCount(2);
            verifier.Verify(0, FileFindResultType.Directory, to("/Some/Path"));
            verifier.Verify(1, FileFindResultType.Directory, to("/Some/Path/In/AnotherpathPlace"));
        }

        [Test]
        public void When_searching_for_a_project_it_will_return()
        {
            var cache = new TypeCache();
            cache.Add(new Project(to("/Some/Path/Project1.cs"), "").SetFileSearch());
            cache.Add(toFile("/Some/Path/File2.cs").SetFileSearch());

            var verifier = new ResultVerifier(cache.FindFiles("Proj"));
            verifier.VerifyCount(1);
            verifier.Verify(0, FileFindResultType.Project, to("/Some/Path/Project1.cs"));
        }

		[Test]
		public void When_not_set_as_file_search_it_will_not_find_it()
		{
			var cache = new TypeCache();
            cache.Add(new Project(to("/Some/Path/Project1.cs"), ""));
            cache.Add(toFile("/Some/Path/File2.cs"));

            var verifier = new ResultVerifier(cache.FindFiles("Proj"));
            verifier.VerifyCount(0);
		}
		
		private ProjectFile toFile(string path)
        {
            return new ProjectFile(to(path), null);
        }
		
		private ProjectFile toFile(string path, string project)
        {
            return new ProjectFile(to(path), project);
        }

		private string to(string path)
		{
			return path.Replace('/', Path.DirectorySeparatorChar);
		}
    }

    class ResultVerifier
    {
        private List<FileFindResult> _list;

        public ResultVerifier(List<FileFindResult> list)
        {
            _list = list;
        }

        public void VerifyCount(int count)
        {
            Assert.That(_list.Count, Is.EqualTo(count));
        }

        public void Verify(int index, FileFindResultType type, string path)
        {
            Assert.That(_list[index].Type, Is.EqualTo(type));
            Assert.That(_list[index].File, Is.EqualTo(path));
            Assert.That(_list[index].ProjectPath, Is.Null);
        }

        public void Verify(int index, FileFindResultType type, string path, string projectPath)
        {
            Assert.That(_list[index].Type, Is.EqualTo(type));
            Assert.That(_list[index].File, Is.EqualTo(path));
            Assert.That(_list[index].ProjectPath, Is.EqualTo(projectPath));
        }
    }
}
