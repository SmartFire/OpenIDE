using System;
using System.Collections.Generic;
using OpenIDE.CodeEngine.Core.Caching.Search;
using OpenIDE.Core.Caching;
namespace OpenIDE.CodeEngine.Core.Caching
{
	public interface ITypeCache
	{
		int ProjectCount { get; }
		int FileCount { get; }
		int CodeReferences { get; }
		
		IEnumerable<Project> AllProjects();
		IEnumerable<ProjectFile> AllFiles();
		IEnumerable<ICodeReference> AllReferences();
		IEnumerable<ISignatureReference> AllSignatures();

		List<ICodeReference> Find(string name);
        List<ICodeReference> Find(string name, int limit);
        List<FileFindResult> FindFiles(string searchString);
        List<FileFindResult> GetFilesInDirectory(string directory);
        List<FileFindResult> GetFilesInProject(string project);
        List<FileFindResult> GetFilesInProject(string project, string path);
    }
}

