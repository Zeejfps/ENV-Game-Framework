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
}
