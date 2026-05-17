using System.Diagnostics;
using LibGit2Sharp;

namespace GitGui;

public sealed class GitService : IGitService
{
    public CommitSnapshot Load(Repo repo, int cap)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return Error(repo, "Not a git repository.");

            using var lg = new Repository(repo.Path);

            var headTip = lg.Head?.Tip;
            var headSha = headTip?.Sha;

            var refTips = new List<Commit>();
            var refsBySha = new Dictionary<string, List<RefBadge>>();

            foreach (var branch in lg.Branches)
            {
                var tip = branch.Tip;
                if (tip == null) continue;
                refTips.Add(tip);
                var kind = branch.IsRemote ? RefKind.RemoteBranch : RefKind.LocalBranch;
                AddBadge(refsBySha, tip.Sha, new RefBadge(branch.FriendlyName, kind));
            }

            if (headSha != null)
                AddBadge(refsBySha, headSha, new RefBadge("HEAD", RefKind.Head));

            if (refTips.Count == 0 && headTip != null)
                refTips.Add(headTip);

            var filter = new CommitFilter
            {
                IncludeReachableFrom = refTips,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time,
            };

            var commitsRaw = new List<Commit>(cap);
            var truncated = false;
            foreach (var c in lg.Commits.QueryBy(filter))
            {
                if (commitsRaw.Count >= cap)
                {
                    truncated = true;
                    break;
                }
                commitsRaw.Add(c);
            }

            var inputs = new LaneAssigner.Input[commitsRaw.Count];
            for (var i = 0; i < commitsRaw.Count; i++)
            {
                var c = commitsRaw[i];
                var parentShas = c.Parents.Select(p => p.Sha).ToArray();
                inputs[i] = new LaneAssigner.Input(c.Sha, parentShas);
            }

            var (assignments, laneCount) = LaneAssigner.Assign(inputs);

            var nodes = new CommitNode[commitsRaw.Count];
            for (var i = 0; i < commitsRaw.Count; i++)
            {
                var c = commitsRaw[i];
                var a = assignments[i];
                var parentShas = (IReadOnlyList<string>)inputs[i].ParentShas;
                refsBySha.TryGetValue(c.Sha, out var badges);

                var inWalkParents = new ParentLink[a.InWalkParentLanes.Length];
                for (var k = 0; k < a.InWalkParentLanes.Length; k++)
                {
                    var p = a.InWalkParentLanes[k];
                    inWalkParents[k] = new ParentLink(p.ParentIndex, p.Lane);
                }

                nodes[i] = new CommitNode(
                    Sha: c.Sha,
                    Summary: c.MessageShort ?? string.Empty,
                    Author: c.Author?.Name ?? string.Empty,
                    When: c.Author?.When ?? c.Committer?.When ?? DateTimeOffset.MinValue,
                    ParentShas: parentShas,
                    Lane: a.Lane,
                    HasIncomingAtCommitLane: a.HasIncomingAtCommitLane,
                    InWalkParentLanes: inWalkParents,
                    IncomingLanes: a.IncomingLanes,
                    PassThroughLanes: a.PassThroughLanes,
                    Refs: badges ?? (IReadOnlyList<RefBadge>)Array.Empty<RefBadge>());
            }

            return new CommitSnapshot(repo.Id, repo.Path, nodes, laneCount, truncated, null);
        }
        catch (Exception ex)
        {
            return Error(repo, ex.Message);
        }
    }

    private static CommitSnapshot Error(Repo repo, string message)
        => new(repo.Id, repo.Path, Array.Empty<CommitNode>(), 0, false, message);

    private static void AddBadge(Dictionary<string, List<RefBadge>> map, string sha, RefBadge badge)
    {
        if (!map.TryGetValue(sha, out var list))
        {
            list = new List<RefBadge>();
            map[sha] = list;
        }
        list.Add(badge);
    }

    public CommitDetails LoadDetails(Repo repo, string sha)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return DetailsError(repo, sha, "Not a git repository.");

            using var lg = new Repository(repo.Path);
            var commit = lg.Lookup<Commit>(sha);
            if (commit == null)
                return DetailsError(repo, sha, "Commit not found.");

            var parentTree = commit.Parents.FirstOrDefault()?.Tree;
            var changes = lg.Diff.Compare<TreeChanges>(parentTree, commit.Tree);

            var files = new List<FileChange>(changes.Count());
            foreach (var entry in changes)
            {
                files.Add(new FileChange(
                    entry.Path,
                    entry.OldPath != entry.Path ? entry.OldPath : null,
                    MapStatus(entry.Status)));
            }
            files.Sort(static (a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));

            return new CommitDetails(
                RepoId: repo.Id,
                Sha: commit.Sha,
                AuthorName: commit.Author?.Name ?? string.Empty,
                AuthorEmail: commit.Author?.Email ?? string.Empty,
                AuthorWhen: commit.Author?.When ?? DateTimeOffset.MinValue,
                CommitterName: commit.Committer?.Name ?? string.Empty,
                CommitterEmail: commit.Committer?.Email ?? string.Empty,
                CommitterWhen: commit.Committer?.When ?? DateTimeOffset.MinValue,
                Message: commit.Message ?? string.Empty,
                MessageShort: commit.MessageShort ?? string.Empty,
                ParentShas: commit.Parents.Select(p => p.Sha).ToArray(),
                Files: files,
                ErrorMessage: null);
        }
        catch (Exception ex)
        {
            return DetailsError(repo, sha, ex.Message);
        }
    }

    private static FileChangeStatus MapStatus(ChangeKind kind) => kind switch
    {
        ChangeKind.Added => FileChangeStatus.Added,
        ChangeKind.Deleted => FileChangeStatus.Deleted,
        ChangeKind.Modified => FileChangeStatus.Modified,
        ChangeKind.Renamed => FileChangeStatus.Renamed,
        ChangeKind.Copied => FileChangeStatus.Copied,
        ChangeKind.TypeChanged => FileChangeStatus.TypeChanged,
        _ => FileChangeStatus.Unmodified,
    };

    public BranchListing GetBranches(Repo repo)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return new BranchListing(repo.Id, Array.Empty<BranchEntry>(), Array.Empty<RemoteGroup>(), "Not a git repository.");

            using var lg = new Repository(repo.Path);

            var headCanonical = lg.Head?.CanonicalName;

            var locals = new List<BranchEntry>();
            var remotesByName = new Dictionary<string, List<BranchEntry>>(StringComparer.Ordinal);
            foreach (var remote in lg.Network.Remotes)
                remotesByName[remote.Name] = new List<BranchEntry>();

            foreach (var branch in lg.Branches)
            {
                var tip = branch.Tip;
                if (tip == null) continue;

                if (branch.IsRemote)
                {
                    var remoteName = branch.RemoteName;
                    if (string.IsNullOrEmpty(remoteName)) continue;

                    // FriendlyName is e.g. "origin/main" — strip the remote prefix for display.
                    var display = branch.FriendlyName;
                    var prefix = remoteName + "/";
                    if (display.StartsWith(prefix, StringComparison.Ordinal))
                        display = display[prefix.Length..];

                    // Skip the symbolic origin/HEAD ref; it just mirrors another branch.
                    if (display == "HEAD") continue;

                    if (!remotesByName.TryGetValue(remoteName, out var list))
                    {
                        list = new List<BranchEntry>();
                        remotesByName[remoteName] = list;
                    }
                    list.Add(new BranchEntry(display, tip.Sha, IsHead: false));
                }
                else
                {
                    var isHead = headCanonical != null && branch.CanonicalName == headCanonical;
                    var tracking = branch.TrackingDetails;
                    locals.Add(new BranchEntry(
                        branch.FriendlyName,
                        tip.Sha,
                        isHead,
                        AheadBy: tracking?.AheadBy,
                        BehindBy: tracking?.BehindBy));
                }
            }

            locals.Sort((a, b) =>
            {
                if (a.IsHead != b.IsHead) return a.IsHead ? -1 : 1;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            var remoteGroups = new List<RemoteGroup>(remotesByName.Count);
            foreach (var kv in remotesByName.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                kv.Value.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                remoteGroups.Add(new RemoteGroup(kv.Key, kv.Value));
            }

            return new BranchListing(repo.Id, locals, remoteGroups, null);
        }
        catch (Exception ex)
        {
            return new BranchListing(repo.Id, Array.Empty<BranchEntry>(), Array.Empty<RemoteGroup>(), ex.Message);
        }
    }

    public LocalChangesSnapshot GetLocalChanges(Repo repo)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return LocalChangesError(repo, "Not a git repository.");

            using var lg = new Repository(repo.Path);
            var status = lg.RetrieveStatus(new StatusOptions
            {
                IncludeUntracked = true,
                RecurseUntrackedDirs = true,
                DetectRenamesInIndex = true,
                DetectRenamesInWorkDir = true,
            });

            var staged = new List<FileChange>();
            var unstaged = new List<FileChange>();

            foreach (var entry in status)
            {
                if (entry.State == FileStatus.Ignored || entry.State == FileStatus.Unaltered)
                    continue;

                var indexStatus = MapIndexStatus(entry.State);
                if (indexStatus != null)
                {
                    var oldPath = entry.HeadToIndexRenameDetails?.OldFilePath;
                    if (oldPath == entry.FilePath) oldPath = null;
                    staged.Add(new FileChange(entry.FilePath, oldPath, indexStatus.Value));
                }

                var workStatus = MapWorkDirStatus(entry.State);
                if (workStatus != null)
                {
                    var oldPath = entry.IndexToWorkDirRenameDetails?.OldFilePath;
                    if (oldPath == entry.FilePath) oldPath = null;
                    unstaged.Add(new FileChange(entry.FilePath, oldPath, workStatus.Value));
                }
            }

            staged.Sort(static (a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));
            unstaged.Sort(static (a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));

            return new LocalChangesSnapshot(repo.Id, staged, unstaged, null);
        }
        catch (Exception ex)
        {
            return LocalChangesError(repo, ex.Message);
        }
    }

    private static FileChangeStatus? MapIndexStatus(FileStatus state)
    {
        if ((state & FileStatus.NewInIndex) != 0) return FileChangeStatus.Added;
        if ((state & FileStatus.ModifiedInIndex) != 0) return FileChangeStatus.Modified;
        if ((state & FileStatus.DeletedFromIndex) != 0) return FileChangeStatus.Deleted;
        if ((state & FileStatus.RenamedInIndex) != 0) return FileChangeStatus.Renamed;
        if ((state & FileStatus.TypeChangeInIndex) != 0) return FileChangeStatus.TypeChanged;
        return null;
    }

    private static FileChangeStatus? MapWorkDirStatus(FileStatus state)
    {
        if ((state & FileStatus.NewInWorkdir) != 0) return FileChangeStatus.Added;
        if ((state & FileStatus.ModifiedInWorkdir) != 0) return FileChangeStatus.Modified;
        if ((state & FileStatus.DeletedFromWorkdir) != 0) return FileChangeStatus.Deleted;
        if ((state & FileStatus.RenamedInWorkdir) != 0) return FileChangeStatus.Renamed;
        if ((state & FileStatus.TypeChangeInWorkdir) != 0) return FileChangeStatus.TypeChanged;
        return null;
    }

    private static LocalChangesSnapshot LocalChangesError(Repo repo, string message)
        => new(repo.Id, Array.Empty<FileChange>(), Array.Empty<FileChange>(), message);

    public void Stage(Repo repo, IReadOnlyList<string> paths)
    {
        if (paths.Count == 0) return;
        using var lg = new Repository(repo.Path);
        Commands.Stage(lg, paths);
    }

    public void Unstage(Repo repo, IReadOnlyList<string> paths)
    {
        if (paths.Count == 0) return;
        using var lg = new Repository(repo.Path);
        Commands.Unstage(lg, paths);
    }

    public string? Commit(Repo repo, string message)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return "Not a git repository.";

            using var lg = new Repository(repo.Path);
            // BuildSignature returns null when user.name / user.email are missing — turn
            // that into a friendly message rather than the ArgumentNullException libgit2
            // would throw if we passed null straight through.
            var sig = lg.Config.BuildSignature(DateTimeOffset.Now);
            if (sig == null)
                return "Set git user.name and user.email before committing.";

            lg.Commit(message, sig, sig);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public PushStatus GetPushStatus(Repo repo)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return new PushStatus(null, HasUpstream: false, Ahead: 0, Behind: 0, IsDetached: false);

            using var lg = new Repository(repo.Path);
            if (lg.Info.IsHeadDetached)
                return new PushStatus(null, HasUpstream: false, Ahead: 0, Behind: 0, IsDetached: true);

            var head = lg.Head;
            var tracked = head?.TrackedBranch;
            var tracking = head?.TrackingDetails;
            return new PushStatus(
                CurrentBranchName: head?.FriendlyName,
                HasUpstream: tracked != null,
                Ahead: tracking?.AheadBy ?? 0,
                Behind: tracking?.BehindBy ?? 0,
                IsDetached: false);
        }
        catch
        {
            return new PushStatus(null, HasUpstream: false, Ahead: 0, Behind: 0, IsDetached: false);
        }
    }

    // Shells out to the `git` CLI so we inherit the user's credential helpers
    // (ssh-agent, osxkeychain, GitHub CLI, …) — libgit2's macOS SSH path is too brittle.
    public PushOutcome Push(Repo repo)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return new PushOutcome(false, "Not a git repository.");

            // Pre-flight: refuse to push from detached HEAD or a branch with no upstream,
            // because the resulting `git push` error is less actionable than these messages.
            using (var lg = new Repository(repo.Path))
            {
                if (lg.Info.IsHeadDetached)
                    return new PushOutcome(false, "HEAD is detached. Check out a branch first.");
                var head = lg.Head;
                if (head?.TrackedBranch == null)
                {
                    var name = head?.FriendlyName ?? "(unknown)";
                    return new PushOutcome(false,
                        $"Branch '{name}' has no upstream. Set one with: git push -u <remote> {name}");
                }
            }

            var psi = new ProcessStartInfo("git", "push")
            {
                WorkingDirectory = repo.Path,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var proc = Process.Start(psi);
            if (proc == null) return new PushOutcome(false, "Failed to start git.");

            // Read both streams concurrently so a full pipe buffer on either side can't deadlock.
            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();
            proc.WaitForExit();
            var stderr = stderrTask.GetAwaiter().GetResult();
            var stdout = stdoutTask.GetAwaiter().GetResult();

            if (proc.ExitCode == 0) return new PushOutcome(true, null);
            var combined = stderr.Length > 0 ? stderr : stdout;
            var msg = FirstMeaningfulLine(combined);
            if (string.IsNullOrEmpty(msg)) msg = $"git push exited with code {proc.ExitCode}.";
            return new PushOutcome(false, msg);
        }
        catch (Exception ex)
        {
            return new PushOutcome(false, ex.Message);
        }
    }

    // Pulls the most relevant single line out of a git error blob — typically the
    // "fatal: …" / "error: …" / "hint: …" line near the end.
    private static string FirstMeaningfulLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var lines = text.Split('\n');
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            if (line.StartsWith("fatal:", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("error:", StringComparison.OrdinalIgnoreCase))
                return line;
        }
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0) return trimmed;
        }
        return text.Trim();
    }

    private static CommitDetails DetailsError(Repo repo, string sha, string message)
        => new(
            RepoId: repo.Id,
            Sha: sha,
            AuthorName: string.Empty,
            AuthorEmail: string.Empty,
            AuthorWhen: DateTimeOffset.MinValue,
            CommitterName: string.Empty,
            CommitterEmail: string.Empty,
            CommitterWhen: DateTimeOffset.MinValue,
            Message: string.Empty,
            MessageShort: string.Empty,
            ParentShas: Array.Empty<string>(),
            Files: Array.Empty<FileChange>(),
            ErrorMessage: message);
}
