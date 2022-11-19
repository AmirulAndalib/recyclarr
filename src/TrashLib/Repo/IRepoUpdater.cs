using System.IO.Abstractions;

namespace TrashLib.Repo;

public interface IRepoUpdater
{
    IDirectoryInfo RepoPath { get; }
    Task UpdateRepo();
}
