using ZGF.Observable;

namespace GitGui;

public interface IRepoRegistry
{
    ObservableList<Repo> Repos { get; }
    ObservableList<Group> Groups { get; }
    State<Repo?> Active { get; }
    State<Guid?> RenamingGroupId { get; }
    State<int> WorktreesChanged { get; }
    void Open(string path);
    void SetActive(Guid id);
    void ToggleGroupCollapsed(Guid groupId);
    Guid CreateGroup(string name);
    void RenameGroup(Guid id, string newName);
    void DeleteGroup(Guid id);
    void MoveRepo(Guid repoId, Guid targetGroupId, int insertIndex);
    void MoveGroup(Guid groupId, int insertIndex);
    void RemoveRepo(Guid repoId);
    void BeginRenameGroup(Guid id);
    void EndRenameGroup();
    BranchesUiState GetBranchesUi(Guid repoId);
    void SetBranchesUi(Guid repoId, BranchesUiState state);
    IEnumerable<Repo> GetWorktrees(Guid primaryId);
    bool IsWorktreeExpanded(Guid primaryId);
    void SetWorktreeExpanded(Guid primaryId, bool expanded);
    void ReplaceWorktreesFor(Guid primaryId, IReadOnlyList<WorktreeDescriptor> desired);
}
