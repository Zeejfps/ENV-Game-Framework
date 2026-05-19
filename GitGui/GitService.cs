using System.Collections.Concurrent;
using System.Diagnostics;
using LibGit2Sharp;

namespace GitGui;

public sealed class GitService : IGitService
{
    // Every git write touches .git/index.lock; two writes against the same repo at the same
    // time (e.g. a checkout from the sidebar racing a stage from the local-changes panel, or
    // an impatient user double-clicking branches) collide with "Unable to create
    // '.git/index.lock': File exists". Serialize all mutating ops per repo so the call sites
    // can't race each other — their own UI-busy flags become cosmetic, not correctness guards.
    // Reads stay unguarded; libgit2/git CLI tolerate concurrent reads, and the next
    // RefsChangedMessage refresh corrects any brief inconsistency.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _repoLocks =
        new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

    private static SemaphoreSlim GetRepoLock(string repoPath)
    {
        string key;
        try { key = Path.GetFullPath(repoPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        catch { key = repoPath; }
        return _repoLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
    }


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

            // Walk stash tips too so stash commits show in the graph. Stash entries are
            // merge commits whose parents include the index/untracked snapshots — those get
            // pulled in automatically via the topological walk.
            var stashIndex = 0;
            foreach (var stash in lg.Stashes)
            {
                var tip = stash.WorkTree;
                if (tip == null) { stashIndex++; continue; }
                refTips.Add(tip);
                var label = StripStashPrefix(stash.Message ?? string.Empty);
                if (string.IsNullOrEmpty(label)) label = $"stash@{{{stashIndex}}}";
                AddBadge(refsBySha, tip.Sha, new RefBadge(label, RefKind.Stash));
                stashIndex++;
            }

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

    // Same AOT-marshalling story as GetDiff: libgit2 callbacks for branch enumeration trip
    // NativeAOT's reverse-pinvoke stubs, so remote branches don't show in published builds.
    // `git for-each-ref` returns the same data in one shot.
    public BranchListing GetBranches(Repo repo)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return new BranchListing(repo.Id, Array.Empty<BranchEntry>(), Array.Empty<RemoteGroup>(), Array.Empty<StashEntry>(), "Not a git repository.");

            // Seed with all configured remotes so groups still show even when a remote has
            // no branches yet (matches the prior LibGit2Sharp behavior).
            var remotesByName = new Dictionary<string, List<BranchEntry>>(StringComparer.Ordinal);
            var remotesOut = RunGit(repo.Path, out var remErr, "remote");
            if (remotesOut == null)
                return new BranchListing(repo.Id, Array.Empty<BranchEntry>(), Array.Empty<RemoteGroup>(), Array.Empty<StashEntry>(), remErr ?? "git remote failed.");
            foreach (var rawLine in remotesOut.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var name = rawLine.Trim();
                if (name.Length > 0) remotesByName[name] = new List<BranchEntry>();
            }

            // Empty if HEAD is detached; we just compare for equality below, so null is fine.
            var headRef = RunGit(repo.Path, out _, "symbolic-ref", "-q", "HEAD")?.Trim();

            const char Sep = '\x1F';
            var fmt = $"%(objectname){Sep}%(refname){Sep}%(upstream:track,nobracket)";
            var branchesOut = RunGit(repo.Path, out var brErr,
                "for-each-ref", $"--format={fmt}", "refs/heads", "refs/remotes");
            if (branchesOut == null)
                return new BranchListing(repo.Id, Array.Empty<BranchEntry>(), Array.Empty<RemoteGroup>(), Array.Empty<StashEntry>(), brErr ?? "git for-each-ref failed.");

            var locals = new List<BranchEntry>();
            foreach (var line in branchesOut.Split('\n'))
            {
                if (line.Length == 0) continue;
                var parts = line.Split(Sep);
                if (parts.Length < 2) continue;
                var sha = parts[0];
                var refname = parts[1];
                var track = parts.Length > 2 ? parts[2] : string.Empty;

                if (refname.StartsWith("refs/heads/", StringComparison.Ordinal))
                {
                    var name = refname["refs/heads/".Length..];
                    var isHead = headRef == refname;
                    var (ahead, behind) = ParseTrack(track);
                    locals.Add(new BranchEntry(name, sha, isHead, AheadBy: ahead, BehindBy: behind));
                }
                else if (refname.StartsWith("refs/remotes/", StringComparison.Ordinal))
                {
                    var rest = refname["refs/remotes/".Length..];
                    var slash = rest.IndexOf('/');
                    if (slash <= 0) continue;
                    var remoteName = rest[..slash];
                    var display = rest[(slash + 1)..];
                    // Skip the symbolic origin/HEAD ref; it just mirrors another branch.
                    if (display == "HEAD") continue;
                    if (!remotesByName.TryGetValue(remoteName, out var list))
                    {
                        list = new List<BranchEntry>();
                        remotesByName[remoteName] = list;
                    }
                    list.Add(new BranchEntry(display, sha, IsHead: false));
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

            var stashes = LoadStashes(repo.Path);
            return new BranchListing(repo.Id, locals, remoteGroups, stashes, null);
        }
        catch (Exception ex)
        {
            return new BranchListing(repo.Id, Array.Empty<BranchEntry>(), Array.Empty<RemoteGroup>(), Array.Empty<StashEntry>(), ex.Message);
        }
    }

    // `git stash list` is the source of truth for stash@{N} indexing; refs/stash only
    // points at the most recent entry. Stash list runs through `git log`, so the format
    // codes are the log ones (%H, %s) — NOT the for-each-ref ones (%(objectname)) which
    // get printed literally here.
    private static IReadOnlyList<StashEntry> LoadStashes(string repoPath)
    {
        const char Sep = '\x1F';
        var fmt = $"%H{Sep}%gs";
        var output = RunGit(repoPath, out _, "stash", "list", $"--format={fmt}");
        if (string.IsNullOrEmpty(output)) return Array.Empty<StashEntry>();

        var list = new List<StashEntry>();
        var idx = 0;
        foreach (var line in output.Split('\n'))
        {
            if (line.Length == 0) continue;
            var parts = line.Split(Sep, 2);
            if (parts.Length < 2) continue;
            list.Add(new StashEntry(idx++, parts[0], StripStashPrefix(parts[1])));
        }
        return list;
    }

    // The reflog subject is "On <branch>: <msg>" (with -m) or
    // "WIP on <branch>: <sha> <commit-subject>" (without). Both are noise — the user
    // cares about the part after the first ": ".
    private static string StripStashPrefix(string reflogSubject)
    {
        var colon = reflogSubject.IndexOf(": ", StringComparison.Ordinal);
        if (colon < 0) return reflogSubject;
        return reflogSubject[(colon + 2)..];
    }

    // `git for-each-ref %(upstream:track,nobracket)` returns "", "gone", "ahead N",
    // "behind N", or "ahead N, behind M".
    private static (int? ahead, int? behind) ParseTrack(string track)
    {
        if (string.IsNullOrEmpty(track) || track == "gone") return (null, null);
        int? a = null, b = null;
        foreach (var part in track.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var p = part.Trim();
            if (p.StartsWith("ahead ", StringComparison.Ordinal) && int.TryParse(p[6..], out var av)) a = av;
            else if (p.StartsWith("behind ", StringComparison.Ordinal) && int.TryParse(p[7..], out var bv)) b = bv;
        }
        return (a, b);
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
                IncludeIgnored = false,
                IncludeUntracked = true,
                RecurseUntrackedDirs = true,
                DetectRenamesInIndex = true,
                DetectRenamesInWorkDir = true,
            });

            var staged = new List<FileChange>();
            var unstaged = new List<FileChange>();

            foreach (var entry in status)
            {
                // Bitwise check: FileStatus is a [Flags] enum, and libgit2 can return an
                // entry with Ignored combined with other bits (e.g., NewInWorkdir on first
                // sight of a file matching a newly-added ignore rule). The previous `==`
                // check let those slip through and they rendered as Added in unstaged.
                if ((entry.State & FileStatus.Ignored) != 0) continue;
                if (entry.State == FileStatus.Unaltered) continue;

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
        var sem = GetRepoLock(repo.Path);
        sem.Wait();
        try
        {
            using var lg = new Repository(repo.Path);
            Commands.Stage(lg, paths);
        }
        finally { sem.Release(); }
    }

    public void Unstage(Repo repo, IReadOnlyList<string> paths)
    {
        if (paths.Count == 0) return;
        var sem = GetRepoLock(repo.Path);
        sem.Wait();
        try
        {
            using var lg = new Repository(repo.Path);
            Commands.Unstage(lg, paths);
        }
        finally { sem.Release(); }
    }

    public void ResetToParent(Repo repo, IReadOnlyList<string> paths)
    {
        if (paths.Count == 0) return;
        var sem = GetRepoLock(repo.Path);
        sem.Wait();
        try
        {
            using var lg = new Repository(repo.Path);
            var tip = lg.Head?.Tip;
            if (tip == null) return;
            var parent = tip.Parents.FirstOrDefault();
            // Mirrors `git reset <parent|root> -- <paths>` by rewriting index entries directly:
            // each path's index entry is replaced with the parent's blob (or removed if the
            // path isn't in the parent / there is no parent). Working tree is untouched.
            foreach (var p in paths)
            {
                var entry = parent?[p];
                if (entry?.Target is Blob blob)
                    lg.Index.Add(blob, p, entry.Mode);
                else
                    lg.Index.Remove(p);
            }
            lg.Index.Write();
        }
        finally { sem.Release(); }
    }

    // Throws away unstaged workdir changes for the given paths. Tracked files are restored
    // from the index via `git checkout -- <paths>` (the user's staged hunks are preserved);
    // untracked files (not in the index) are deleted from disk. Returns null on success or
    // a human-readable error string on failure.
    public string? DiscardChanges(Repo repo, IReadOnlyList<string> paths)
    {
        if (paths.Count == 0) return null;
        try
        {
            if (!Repository.IsValid(repo.Path))
                return "Not a git repository.";

            var sem = GetRepoLock(repo.Path);
            sem.Wait();
            try
            {
                var trackedPaths = new List<string>();
                using (var lg = new Repository(repo.Path))
                {
                    foreach (var p in paths)
                    {
                        // Index presence is the tracked/untracked signal: anything in the
                        // index has had at least one commit or stage and can be restored
                        // from there; anything else exists only on disk.
                        if (lg.Index[p] != null)
                        {
                            trackedPaths.Add(p);
                            continue;
                        }
                        var fullPath = Path.Combine(repo.Path, p);
                        try
                        {
                            if (File.Exists(fullPath)) File.Delete(fullPath);
                            else if (Directory.Exists(fullPath)) Directory.Delete(fullPath, recursive: true);
                        }
                        catch (Exception ex)
                        {
                            return ex.Message;
                        }
                    }
                }

                if (trackedPaths.Count > 0)
                {
                    var args = new List<string> { "checkout", "--" };
                    args.AddRange(trackedPaths);
                    var psi = BuildGitProcessStartInfo(args, repo.Path);
                    using var proc = Process.Start(psi);
                    if (proc == null) return "Failed to start git.";
                    var stdoutTask = proc.StandardOutput.ReadToEndAsync();
                    var stderrTask = proc.StandardError.ReadToEndAsync();
                    proc.WaitForExit();
                    if (proc.ExitCode != 0)
                    {
                        var stderr = stderrTask.GetAwaiter().GetResult();
                        var stdout = stdoutTask.GetAwaiter().GetResult();
                        var combined = stderr.Length > 0 ? stderr : stdout;
                        var msg = FirstMeaningfulLine(combined);
                        return string.IsNullOrEmpty(msg)
                            ? $"git checkout exited with code {proc.ExitCode}."
                            : msg;
                    }
                }
                return null;
            }
            finally { sem.Release(); }
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public string? Commit(Repo repo, string message, bool amend)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return "Not a git repository.";

            var sem = GetRepoLock(repo.Path);
            sem.Wait();
            try
            {
                using var lg = new Repository(repo.Path);
                // BuildSignature returns null when user.name / user.email are missing — turn
                // that into a friendly message rather than the ArgumentNullException libgit2
                // would throw if we passed null straight through.
                var sig = lg.Config.BuildSignature(DateTimeOffset.Now);
                if (sig == null)
                    return "Set git user.name and user.email before committing.";

                if (amend)
                {
                    var tip = lg.Head?.Tip;
                    if (tip == null) return "Nothing to amend — HEAD has no commits.";
                    // Keep the original author (matches `git commit --amend` default); update
                    // the committer + time to the current user.
                    lg.Commit(message, tip.Author, sig, new CommitOptions { AmendPreviousCommit = true });
                }
                else
                {
                    lg.Commit(message, sig, sig);
                }
                return null;
            }
            finally { sem.Release(); }
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public HeadCommitMessage? GetHeadCommitMessage(Repo repo)
    {
        try
        {
            if (!Repository.IsValid(repo.Path)) return null;
            using var lg = new Repository(repo.Path);
            var tip = lg.Head?.Tip;
            if (tip == null) return null;

            var title = (tip.MessageShort ?? string.Empty).Trim();
            var full = (tip.Message ?? string.Empty).Replace("\r\n", "\n");
            // Body is everything after the first blank line (git's subject/body split).
            var body = string.Empty;
            var sepIdx = full.IndexOf("\n\n", StringComparison.Ordinal);
            if (sepIdx >= 0)
                body = full[(sepIdx + 2)..].TrimEnd();
            return new HeadCommitMessage(title, body);
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<FileChange> GetHeadCommitFiles(Repo repo)
    {
        try
        {
            if (!Repository.IsValid(repo.Path)) return Array.Empty<FileChange>();
            using var lg = new Repository(repo.Path);
            var tip = lg.Head?.Tip;
            if (tip == null) return Array.Empty<FileChange>();

            var parentTree = tip.Parents.FirstOrDefault()?.Tree;
            var changes = lg.Diff.Compare<TreeChanges>(parentTree, tip.Tree);
            var files = new List<FileChange>();
            foreach (var entry in changes)
            {
                files.Add(new FileChange(
                    entry.Path,
                    entry.OldPath != entry.Path ? entry.OldPath : null,
                    MapStatus(entry.Status)));
            }
            files.Sort(static (a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));
            return files;
        }
        catch
        {
            return Array.Empty<FileChange>();
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

            var sem = GetRepoLock(repo.Path);
            sem.Wait();
            try
            {
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

                var psi = BuildGitProcessStartInfo("push", repo.Path);
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
            finally { sem.Release(); }
        }
        catch (Exception ex)
        {
            return new PushOutcome(false, ex.Message);
        }
    }

    public PullOutcome Pull(Repo repo)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return new PullOutcome(false, "Not a git repository.");

            var sem = GetRepoLock(repo.Path);
            sem.Wait();
            try
            {
                using (var lg = new Repository(repo.Path))
                {
                    if (lg.Info.IsHeadDetached)
                        return new PullOutcome(false, "HEAD is detached. Check out a branch first.");
                    var head = lg.Head;
                    if (head?.TrackedBranch == null)
                    {
                        var name = head?.FriendlyName ?? "(unknown)";
                        return new PullOutcome(false,
                            $"Branch '{name}' has no upstream. Set one with: git branch --set-upstream-to=<remote>/<branch>");
                    }
                }

                var psi = BuildGitProcessStartInfo("pull", repo.Path);
                using var proc = Process.Start(psi);
                if (proc == null) return new PullOutcome(false, "Failed to start git.");

                var stdoutTask = proc.StandardOutput.ReadToEndAsync();
                var stderrTask = proc.StandardError.ReadToEndAsync();
                proc.WaitForExit();
                var stderr = stderrTask.GetAwaiter().GetResult();
                var stdout = stdoutTask.GetAwaiter().GetResult();

                if (proc.ExitCode == 0) return new PullOutcome(true, null);
                var combined = stderr.Length > 0 ? stderr : stdout;
                var msg = FirstMeaningfulLine(combined);
                if (string.IsNullOrEmpty(msg)) msg = $"git pull exited with code {proc.ExitCode}.";
                return new PullOutcome(false, msg);
            }
            finally { sem.Release(); }
        }
        catch (Exception ex)
        {
            return new PullOutcome(false, ex.Message);
        }
    }

    public FetchOutcome Fetch(Repo repo)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return new FetchOutcome(false, "Not a git repository.");

            var sem = GetRepoLock(repo.Path);
            sem.Wait();
            try
            {
                var psi = BuildGitProcessStartInfo("fetch --all --prune", repo.Path);
                using var proc = Process.Start(psi);
                if (proc == null) return new FetchOutcome(false, "Failed to start git.");

                var stdoutTask = proc.StandardOutput.ReadToEndAsync();
                var stderrTask = proc.StandardError.ReadToEndAsync();
                proc.WaitForExit();
                var stderr = stderrTask.GetAwaiter().GetResult();
                var stdout = stdoutTask.GetAwaiter().GetResult();

                if (proc.ExitCode == 0) return new FetchOutcome(true, null);
                var combined = stderr.Length > 0 ? stderr : stdout;
                var msg = FirstMeaningfulLine(combined);
                if (string.IsNullOrEmpty(msg)) msg = $"git fetch exited with code {proc.ExitCode}.";
                return new FetchOutcome(false, msg);
            }
            finally { sem.Release(); }
        }
        catch (Exception ex)
        {
            return new FetchOutcome(false, ex.Message);
        }
    }

    // Shells out so post-checkout hooks, LFS, and sparse-checkout filters all run; also
    // surfaces the same error wording the user would see in Terminal.
    public CheckoutOutcome CheckoutLocalBranch(Repo repo, string branchName)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return new CheckoutOutcome(false, "Not a git repository.");

            var sem = GetRepoLock(repo.Path);
            sem.Wait();
            try { return RunGitCheckout(repo.Path, new[] { "checkout", branchName }); }
            finally { sem.Release(); }
        }
        catch (Exception ex)
        {
            return new CheckoutOutcome(false, ex.Message);
        }
    }

    public CheckoutOutcome CheckoutRemoteBranch(Repo repo, string localName, string remoteName, string remoteBranchName, bool track)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return new CheckoutOutcome(false, "Not a git repository.");

            var args = new List<string>
            {
                "checkout", "-b", localName,
                track ? "--track" : "--no-track",
                $"{remoteName}/{remoteBranchName}",
            };
            var sem = GetRepoLock(repo.Path);
            sem.Wait();
            try { return RunGitCheckout(repo.Path, args); }
            finally { sem.Release(); }
        }
        catch (Exception ex)
        {
            return new CheckoutOutcome(false, ex.Message);
        }
    }

    // Shells out so post-checkout hooks run when `checkout` is true, and the error wording
    // matches the user's terminal experience (e.g. "fatal: A branch named 'x' already exists.").
    public CreateBranchOutcome CreateBranch(Repo repo, string name, string startPoint, bool checkout)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return new CreateBranchOutcome(false, "Not a git repository.");

            var args = checkout
                ? new List<string> { "checkout", "-b", name, startPoint }
                : new List<string> { "branch", name, startPoint };

            var sem = GetRepoLock(repo.Path);
            sem.Wait();
            try
            {
                var psi = BuildGitProcessStartInfo(args, repo.Path);
                using var proc = Process.Start(psi);
                if (proc == null) return new CreateBranchOutcome(false, "Failed to start git.");

                var stdoutTask = proc.StandardOutput.ReadToEndAsync();
                var stderrTask = proc.StandardError.ReadToEndAsync();
                proc.WaitForExit();
                var stderr = stderrTask.GetAwaiter().GetResult();
                var stdout = stdoutTask.GetAwaiter().GetResult();

                if (proc.ExitCode == 0) return new CreateBranchOutcome(true, null);
                var combined = stderr.Length > 0 ? stderr : stdout;
                var msg = FirstMeaningfulLine(combined);
                if (string.IsNullOrEmpty(msg))
                    msg = $"git {(checkout ? "checkout" : "branch")} exited with code {proc.ExitCode}.";
                return new CreateBranchOutcome(false, msg);
            }
            finally { sem.Release(); }
        }
        catch (Exception ex)
        {
            return new CreateBranchOutcome(false, ex.Message);
        }
    }

    public StashOutcome CreateStash(Repo repo, string message, bool includeUntracked, bool keepIndex)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return new StashOutcome(false, "Not a git repository.");

            var args = new List<string> { "stash", "push" };
            if (includeUntracked) args.Add("--include-untracked");
            if (keepIndex) args.Add("--keep-index");
            if (!string.IsNullOrEmpty(message))
            {
                args.Add("-m");
                args.Add(message);
            }

            var sem = GetRepoLock(repo.Path);
            sem.Wait();
            try { return RunGitStash(repo.Path, args, "git stash push"); }
            finally { sem.Release(); }
        }
        catch (Exception ex)
        {
            return new StashOutcome(false, ex.Message);
        }
    }

    public StashOutcome ApplyStash(Repo repo, int index)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return new StashOutcome(false, "Not a git repository.");

            var args = new List<string> { "stash", "apply", $"stash@{{{index}}}" };
            var sem = GetRepoLock(repo.Path);
            sem.Wait();
            try { return RunGitStash(repo.Path, args, "git stash apply"); }
            finally { sem.Release(); }
        }
        catch (Exception ex)
        {
            return new StashOutcome(false, ex.Message);
        }
    }

    public StashOutcome DropStash(Repo repo, int index)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return new StashOutcome(false, "Not a git repository.");

            var args = new List<string> { "stash", "drop", $"stash@{{{index}}}" };
            var sem = GetRepoLock(repo.Path);
            sem.Wait();
            try { return RunGitStash(repo.Path, args, "git stash drop"); }
            finally { sem.Release(); }
        }
        catch (Exception ex)
        {
            return new StashOutcome(false, ex.Message);
        }
    }

    private static StashOutcome RunGitStash(string repoPath, IReadOnlyList<string> gitArgs, string label)
    {
        var psi = BuildGitProcessStartInfo(gitArgs, repoPath);
        using var proc = Process.Start(psi);
        if (proc == null) return new StashOutcome(false, "Failed to start git.");

        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stderr = stderrTask.GetAwaiter().GetResult();
        var stdout = stdoutTask.GetAwaiter().GetResult();

        if (proc.ExitCode == 0) return new StashOutcome(true, null);
        var combined = stderr.Length > 0 ? stderr : stdout;
        var msg = FirstMeaningfulLine(combined);
        if (string.IsNullOrEmpty(msg)) msg = $"{label} exited with code {proc.ExitCode}.";
        return new StashOutcome(false, msg);
    }

    private static CheckoutOutcome RunGitCheckout(string repoPath, IReadOnlyList<string> gitArgs)
    {
        var psi = BuildGitProcessStartInfo(gitArgs, repoPath);
        using var proc = Process.Start(psi);
        if (proc == null) return new CheckoutOutcome(false, "Failed to start git.");

        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stderr = stderrTask.GetAwaiter().GetResult();
        var stdout = stdoutTask.GetAwaiter().GetResult();

        if (proc.ExitCode == 0) return new CheckoutOutcome(true, null);
        var combined = stderr.Length > 0 ? stderr : stdout;
        var msg = FirstMeaningfulLine(combined);
        if (string.IsNullOrEmpty(msg)) msg = $"git checkout exited with code {proc.ExitCode}.";
        return new CheckoutOutcome(false, msg);
    }

    // On macOS, GUI apps launched outside a terminal (Finder, IDE, Launch Services)
    // don't inherit the user's interactive-shell environment. Anything set up in
    // .zshrc / .bashrc — 1Password's SSH_AUTH_SOCK, manually-started ssh-agent, the
    // Homebrew PATH, GIT_SSH_COMMAND overrides — is invisible to the child process,
    // and `git push` over SSH dies with "Could not read from remote repository".
    //
    // Running git through the user's shell with `-i -c` sources their rc files first
    // so ssh and git see the same environment they do in Terminal.
    private static ProcessStartInfo BuildGitProcessStartInfo(string gitArgs, string workingDir)
    {
        var psi = new ProcessStartInfo
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (OperatingSystem.IsMacOS())
        {
            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (string.IsNullOrEmpty(shell)) shell = "/bin/zsh";
            psi.FileName = shell;
            psi.ArgumentList.Add("-i");
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add($"git {gitArgs}");
        }
        else
        {
            psi.FileName = "git";
            foreach (var part in gitArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                psi.ArgumentList.Add(part);
        }

        return psi;
    }

    // Args-list variant for callers passing user-typed strings (e.g. a branch name from a
    // dialog): each arg is shell-quoted on the macOS `sh -c` path so spaces or metacharacters
    // can't break the command or inject extra ones.
    private static ProcessStartInfo BuildGitProcessStartInfo(IReadOnlyList<string> gitArgs, string workingDir)
    {
        var psi = new ProcessStartInfo
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (OperatingSystem.IsMacOS())
        {
            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (string.IsNullOrEmpty(shell)) shell = "/bin/zsh";
            psi.FileName = shell;
            psi.ArgumentList.Add("-i");
            psi.ArgumentList.Add("-c");
            var sb = new System.Text.StringBuilder("git");
            foreach (var a in gitArgs)
            {
                sb.Append(' ');
                sb.Append(SingleQuoteShellArg(a));
            }
            psi.ArgumentList.Add(sb.ToString());
        }
        else
        {
            psi.FileName = "git";
            foreach (var a in gitArgs) psi.ArgumentList.Add(a);
        }

        return psi;
    }

    private static string SingleQuoteShellArg(string s)
        => "'" + s.Replace("'", "'\\''") + "'";

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

    // LibGit2Sharp's Patch API drives diff output through native→managed callbacks (per
    // hunk and per line), which the NativeAOT-generated marshalling stubs for GitDiffHunk
    // NRE on. Everything else in libgit2 we use is fine; only diff goes through this
    // callback path. Shell out to `git diff` for diffs to sidestep it entirely.
    public DiffResult GetDiff(Repo repo, string path, DiffSide side)
    {
        try
        {
            if (!Repository.IsValid(repo.Path))
                return DiffError(repo, path, side, "Not a git repository.");

            var contextArg = $"--unified={DiffOptions.ContextLines}";
            string? patchText;
            string? error;

            if (side == DiffSide.Staged)
            {
                patchText = RunGitDiff(repo.Path, out error,
                    "diff", "--cached", "--no-color", "-M", contextArg, "--", path);
            }
            else if (IsTracked(repo.Path, path))
            {
                patchText = RunGitDiff(repo.Path, out error,
                    "diff", "--no-color", "-M", contextArg, "--", path);
            }
            else
            {
                // Untracked file: `git diff` ignores it, so render it as an addition by
                // diffing against the platform null device.
                var nullPath = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
                var absPath = Path.IsPathRooted(path) ? path : Path.Combine(repo.Path, path);
                patchText = RunGitDiff(repo.Path, out error,
                    "diff", "--no-color", "--no-index", contextArg, "--", nullPath, absPath);
            }

            if (patchText == null)
                return DiffError(repo, path, side, error ?? "git diff failed.");

            return ParseGitDiff(repo.Id, path, side, patchText);
        }
        catch (Exception ex)
        {
            return DiffError(repo, path, side, ex.Message);
        }
    }

    private static string RunGit(string workingDir, out string? error, params string[] args)
        => RunGitInternal(workingDir, allowExitCode1: false, out error, args)!;

    // `git diff --no-index` exits 1 when the two inputs differ — that's normal output, not failure.
    private static string? RunGitDiff(string workingDir, out string? error, params string[] args)
        => RunGitInternal(workingDir, allowExitCode1: true, out error, args);

    private static string? RunGitInternal(string workingDir, bool allowExitCode1, out string? error, string[] args)
    {
        error = null;
        var psi = new ProcessStartInfo
        {
            FileName = GitExecutable(),
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi);
        if (proc == null) { error = "Failed to start git."; return null; }

        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();

        if (proc.ExitCode == 0 || (allowExitCode1 && proc.ExitCode == 1)) return stdout;

        var msg = FirstMeaningfulLine(stderr);
        error = string.IsNullOrEmpty(msg) ? $"git exited with code {proc.ExitCode}." : msg;
        return null;
    }

    private static bool IsTracked(string workingDir, string path)
    {
        var psi = new ProcessStartInfo
        {
            FileName = GitExecutable(),
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("ls-files");
        psi.ArgumentList.Add("--error-unmatch");
        psi.ArgumentList.Add("--");
        psi.ArgumentList.Add(path);

        using var proc = Process.Start(psi);
        if (proc == null) return false;
        proc.StandardOutput.ReadToEnd();
        proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return proc.ExitCode == 0;
    }

    // macOS GUI apps launched outside a terminal don't inherit the user's shell PATH, so
    // Homebrew git (/opt/homebrew/bin/git, /usr/local/bin/git) is invisible to a bare
    // Process.Start("git"). Ask the login shell where git lives, once, and reuse the
    // absolute path everywhere.
    private static string? _gitExecutable;
    private static readonly object _gitExecutableLock = new();

    private static string GitExecutable()
    {
        if (_gitExecutable != null) return _gitExecutable;
        lock (_gitExecutableLock)
        {
            _gitExecutable ??= ResolveGitExecutable();
            return _gitExecutable;
        }
    }

    private static string ResolveGitExecutable()
    {
        if (!OperatingSystem.IsMacOS()) return "git";

        try
        {
            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (string.IsNullOrEmpty(shell)) shell = "/bin/zsh";
            var psi = new ProcessStartInfo
            {
                FileName = shell,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-l");
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add("command -v git");
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var path = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit();
                if (path.Length > 0 && File.Exists(path)) return path;
            }
        }
        catch { /* fall through to defaults */ }

        foreach (var p in new[] { "/opt/homebrew/bin/git", "/usr/local/bin/git", "/usr/bin/git" })
            if (File.Exists(p)) return p;

        return "git";
    }

    private static DiffResult ParseGitDiff(Guid repoId, string path, DiffSide side, string patchText)
    {
        if (string.IsNullOrEmpty(patchText))
            return new DiffResult(repoId, path, null, side, false, false, null, null, Array.Empty<DiffHunk>(), false, null);

        string? oldPath = null;
        int? oldMode = null, newMode = null;
        bool isBinary = false;

        foreach (var rawLine in patchText.Replace("\r\n", "\n").Split('\n'))
        {
            if (rawLine.StartsWith("@@")) break;
            if (rawLine.StartsWith("rename from "))
                oldPath = rawLine.Substring("rename from ".Length).Trim();
            else if (rawLine.StartsWith("old mode "))
                oldMode = TryParseOctal(rawLine.Substring("old mode ".Length).Trim());
            else if (rawLine.StartsWith("new mode "))
                newMode = TryParseOctal(rawLine.Substring("new mode ".Length).Trim());
            else if (rawLine.StartsWith("Binary files ") || rawLine.StartsWith("GIT binary patch"))
                isBinary = true;
        }

        if (isBinary)
            return new DiffResult(repoId, path, oldPath, side, true, false, oldMode, newMode, Array.Empty<DiffHunk>(), false, null);

        var (hunks, truncated) = ParsePatch(patchText);
        var modesDiffer = oldMode.HasValue && newMode.HasValue && oldMode != newMode;
        var isModeOnly = modesDiffer && hunks.Count == 0;

        return new DiffResult(
            RepoId: repoId,
            Path: path,
            OldPath: oldPath,
            Side: side,
            IsBinary: false,
            IsModeOnly: isModeOnly,
            OldMode: modesDiffer ? oldMode : null,
            NewMode: modesDiffer ? newMode : null,
            Hunks: hunks,
            Truncated: truncated,
            ErrorMessage: null);
    }

    private static int? TryParseOctal(string s)
    {
        try { return Convert.ToInt32(s, 8); }
        catch { return null; }
    }

    private static DiffResult DiffError(Repo repo, string path, DiffSide side, string message)
        => new(repo.Id, path, null, side, false, false, null, null, Array.Empty<DiffHunk>(), false, message);

    private static (IReadOnlyList<DiffHunk> Hunks, bool Truncated) ParsePatch(string patchText)
    {
        var hunks = new List<DiffHunk>();
        if (string.IsNullOrEmpty(patchText))
            return (hunks, false);

        var totalLines = 0;
        var truncated = false;

        // Build the current hunk incrementally as we walk the patch text.
        int curOldStart = 0, curOldLines = 0, curNewStart = 0, curNewLines = 0;
        string? curHeader = null;
        List<DiffLine>? curLines = null;
        int oldLineCursor = 0, newLineCursor = 0;
        bool inHunk = false;

        void Flush(List<DiffHunk> dst)
        {
            if (!inHunk || curLines == null) return;
            dst.Add(new DiffHunk(curOldStart, curOldLines, curNewStart, curNewLines, curHeader, curLines));
        }

        foreach (var raw in patchText.Replace("\r\n", "\n").Split('\n'))
        {
            if (raw.StartsWith("@@"))
            {
                Flush(hunks);
                if (!TryParseHunkHeader(raw, out curOldStart, out curOldLines, out curNewStart, out curNewLines, out curHeader))
                {
                    inHunk = false;
                    continue;
                }
                curLines = new List<DiffLine>();
                oldLineCursor = curOldStart;
                newLineCursor = curNewStart;
                inHunk = true;
                continue;
            }

            if (!inHunk || curLines == null) continue;
            if (raw.Length == 0) continue;
            if (raw[0] == '\\') continue;

            DiffLine? line = null;
            switch (raw[0])
            {
                case ' ':
                    line = new DiffLine(DiffLineKind.Context, oldLineCursor, newLineCursor, raw.Length > 1 ? raw[1..] : string.Empty);
                    oldLineCursor++;
                    newLineCursor++;
                    break;
                case '+':
                    line = new DiffLine(DiffLineKind.Added, null, newLineCursor, raw.Length > 1 ? raw[1..] : string.Empty);
                    newLineCursor++;
                    break;
                case '-':
                    line = new DiffLine(DiffLineKind.Removed, oldLineCursor, null, raw.Length > 1 ? raw[1..] : string.Empty);
                    oldLineCursor++;
                    break;
            }

            if (line == null) continue;
            if (totalLines >= DiffOptions.TruncationLineCap)
            {
                truncated = true;
                continue;
            }
            curLines.Add(line);
            totalLines++;
        }

        Flush(hunks);
        return (hunks, truncated);
    }

    // Parses "@@ -<oldStart>[,<oldLines>] +<newStart>[,<newLines>] @@ <header?>".
    private static bool TryParseHunkHeader(
        string raw,
        out int oldStart, out int oldLines,
        out int newStart, out int newLines,
        out string? header)
    {
        oldStart = oldLines = newStart = newLines = 0;
        header = null;

        var close = raw.IndexOf("@@", 2, StringComparison.Ordinal);
        if (close < 0) return false;
        var ranges = raw.Substring(2, close - 2).Trim();
        var parts = ranges.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;
        if (parts[0].Length < 2 || parts[0][0] != '-') return false;
        if (parts[1].Length < 2 || parts[1][0] != '+') return false;

        if (!TryParseRange(parts[0].AsSpan(1), out oldStart, out oldLines)) return false;
        if (!TryParseRange(parts[1].AsSpan(1), out newStart, out newLines)) return false;

        var afterClose = close + 2;
        if (afterClose < raw.Length)
        {
            var trail = raw[afterClose..].TrimStart();
            if (trail.Length > 0) header = trail;
        }
        return true;
    }

    private static bool TryParseRange(ReadOnlySpan<char> s, out int start, out int count)
    {
        start = 0;
        count = 1;
        var comma = s.IndexOf(',');
        if (comma < 0)
            return int.TryParse(s, out start);
        if (!int.TryParse(s[..comma], out start)) return false;
        if (!int.TryParse(s[(comma + 1)..], out count)) return false;
        return true;
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
