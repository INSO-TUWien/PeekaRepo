using PeekaRepo.Entities;
using Microsoft.AspNetCore.Components;
using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PeekaRepo.Entities
{
    public class GitApiConnector
    {
        private GitHubClient mGitHubClient { get; }
        private IReadOnlyList<Branch> mBranches;
        private Dictionary<string, IReadOnlyList<GitHubCommit>> mBranchCommits = new Dictionary<string, IReadOnlyList<GitHubCommit>>();
        private string mOwner;
        private string mRepository;
        public Dictionary<string, GitHubCommit> BranchLastCommits { get; private set; } = null;

        public bool IsConnected 
        { 
            get
            {
                return !string.IsNullOrEmpty(mOwner) && !string.IsNullOrEmpty(mRepository);
            } 
        }

        public GitApiConnector(GitHubClient gitHubClient)
        {
            mGitHubClient = gitHubClient;
        }
        
        public async Task<string> Connect(string owner, string repository, string username, string password)
        {
            try
            {
                mGitHubClient.Credentials = new Credentials(username, password);

                return await Connect(owner, repository);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public async Task<string> Connect(string owner, string repository, string token)
        {
            try
            {
                if(!string.IsNullOrWhiteSpace(token))
                {
                    mGitHubClient.Credentials = new Credentials(token);
                }
                else
                {
                    //TODO: reset credentials to null?!
                }


                return await Connect(owner, repository);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        private async Task<string> Connect(string owner, string repository)
        {
            try
            {
                var branchesTask = mGitHubClient.Repository.Branch.GetAll(owner, repository);
                mBranches = await branchesTask;
                BranchLastCommits = null;
                mOwner = owner;
                mRepository = repository;

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public async Task<CompareSummary> AnalyzeReferenceBranch(string branchName)
        {
            var result = await AnalyzeBranch(branchName, null);
            return result;
        }
        public async Task<IReadOnlyList<Octokit.Branch>> GetBranches()
        {
            if (mBranches == null)
            {
                var branchesTask = mGitHubClient.Repository.Branch.GetAll(mOwner, mRepository);
                mBranches = await branchesTask;
            }

            if(BranchLastCommits == null)
            {
                BranchLastCommits = new Dictionary<string, GitHubCommit>();
                foreach (var branch in mBranches)
                {
                    BranchLastCommits.Add(branch.Name, await mGitHubClient.Repository.Commit.Get(mOwner, mRepository, branch.Commit.Sha));
                }
            }

            return mBranches;
        }

        public async Task AnalyzeSingleBranch(string branchName, CompareSummary summary)
        {
            if(!summary.Ahead.ContainsKey(branchName))
            {
                var baseCompare = mGitHubClient.Repository.Commit.Compare(mOwner, mRepository, summary.BranchName, branchName);
                var headCompare = mGitHubClient.Repository.Commit.Compare(mOwner, mRepository, branchName, summary.BranchName);

                var baseResult = await baseCompare;//ahead of name to sha -> behind info
                var headResult = await headCompare;//ahead of sha to name -> ahead info

                summary.Ahead.Add(branchName, baseResult);
                summary.Behind.Add(branchName, headResult);

                List<string> conflictFiles = new List<string>();
                foreach (var file in baseResult.Files)
                {
                    if (headResult.Files.Where(f => f.Filename.Equals(file.Filename)).Any())
                    {
                        conflictFiles.Add(file.Filename);
                    }
                }

                summary.FileConflicts.Add(branchName, conflictFiles);

                CalculateSumAndIdirectConflicts(summary);
            }
        }

        public void RemoveBranchFromSummary(CompareSummary summary, string branchName)
        {
            summary.Ahead.Remove(branchName);
            summary.Behind.Remove(branchName);
            summary.FileConflicts.Remove(branchName);
            CalculateSumAndIdirectConflicts(summary);
        }

        public void CalculateSumAndIdirectConflicts(CompareSummary summary)
        {
            #region calculate sum and indirect conflicts

            int aheadSum = summary.Ahead.Values.Sum(f => f.AheadBy);
            int behindSum = summary.Behind.Values.Sum(f => f.AheadBy);

            HashSet<string> allFileConflicts = new HashSet<string>();
            foreach(string branch in summary.Behind.Keys)
            {
                foreach(var file in summary.FileConflicts[branch])
                {
                    allFileConflicts.Add(file);
                }
            }

            summary.IndirectConflicts = new Dictionary<string, List<string>>();
            for (int i = 0; i < mBranches.Count - 1; i++)
            {
                if (!mBranches[i].Name.Equals(summary.BranchName))
                {
                    for (int j = i + 1; j < mBranches.Count; j++)
                    {
                        if (!mBranches[j].Name.Equals(summary.BranchName) && summary.Ahead.ContainsKey(mBranches[i].Name) && summary.Ahead.ContainsKey(mBranches[j].Name))
                        {
                            List<string> conflictFiles = new List<string>();
                            foreach (var file in summary.Ahead[mBranches[i].Name].Files)
                            {
                                if (!allFileConflicts.Contains(file.Filename))
                                {
                                    if (summary.Ahead[mBranches[j].Name].Files.Where(f => f.Filename.Equals(file.Filename)).Any())
                                    {
                                        if (!summary.IndirectConflicts.ContainsKey(file.Filename))
                                        {
                                            summary.IndirectConflicts.Add(file.Filename, new List<string>());
                                        }
                                        if (!summary.IndirectConflicts[file.Filename].Contains(mBranches[i].Name))
                                        {
                                            summary.IndirectConflicts[file.Filename].Add(mBranches[i].Name);
                                        }
                                        if (!summary.IndirectConflicts[file.Filename].Contains(mBranches[j].Name))
                                        {
                                            summary.IndirectConflicts[file.Filename].Add(mBranches[j].Name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            #endregion

            BranchHierarchy master = CreateHierarchy();
            CheckMerges(summary.BranchName, master, summary);
        }

        private async Task<CompareSummary> AnalyzeBranch(string sha, List<CherryPick> cherryPicks)
        {
            CompareSummary summary = new CompareSummary() { BranchName = sha };
     
            #region create compare summary for branch

            HashSet<string> allFileConflicts = new HashSet<string>();
            for (int i = 0; i < mBranches.Count; i++)
            {
                if (!mBranches[i].Name.Equals(sha) && BranchLastCommits[mBranches[i].Name].Commit.Committer.Date > DateTime.Now.AddMonths(-6))
                {
                    await AnalyzeSingleBranch(mBranches[i].Name, summary);
                }
            }

            #endregion

            CalculateSumAndIdirectConflicts(summary);

            #region load cherry pick data for branch

            if (cherryPicks != null)
            {
                //var cherryPicks = await GetCherryPicks();
                Dictionary<string, List<CherryPick>> branchCherryPicks = new Dictionary<string, List<CherryPick>>();
                for (int i = 0; i < mBranches.Count - 1; i++)
                {
                    foreach (var cherry in cherryPicks)
                    {
                        if (mBranchCommits[mBranches[i].Name].Any(f => f.Sha.Equals(cherry.CherryPickCommit.Sha))) //cherry commit exists in branch
                        {
                            if (!mBranchCommits[mBranches[i].Name].Any(f => f.Sha.Equals(cherry.OriginalCommit.Sha))) //original commit not in branch
                            {
                                if (!branchCherryPicks.ContainsKey(mBranches[i].Name))
                                {
                                    branchCherryPicks.Add(mBranches[i].Name, new List<CherryPick>());
                                }
                                branchCherryPicks[mBranches[i].Name].Add(cherry);
                                if (string.IsNullOrEmpty(cherry.OriginalBranch))
                                {
                                    foreach (string key in mBranchCommits.Keys)
                                    {
                                        if (key != sha)
                                        {
                                            foreach (var com in mBranchCommits[key])
                                            {
                                                if (com.Sha.Equals(cherry.OriginalCommit.Sha))
                                                {
                                                    cherry.OriginalBranch = key;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            #endregion
               
            return summary;
        }

        private void CheckMerges(string sha, BranchHierarchy master, CompareSummary summary)
        {
            summary.WrongMergedCommits = new Dictionary<string, List<GitHubCommit>>();
            #region check invalid merges

            string parentBranch = GetParentBranch(master, sha);
            if (!string.IsNullOrEmpty(parentBranch))
            {
                var branchesToCheck = GetIndirectTopBranchConnections(master, sha);
                foreach (string branch in branchesToCheck)
                {
                    if(summary.Behind.ContainsKey(parentBranch) && summary.Behind.ContainsKey(branch))
                    {
                        foreach (var commit in summary.Behind[parentBranch].Commits)
                        {
                            if (!summary.Behind[branch].Commits.Any(f => f.Sha == commit.Sha))
                            {
                                if (!summary.WrongMergedCommits.ContainsKey(branch))
                                {
                                    summary.WrongMergedCommits.Add(branch, new List<GitHubCommit>());
                                }
                                summary.WrongMergedCommits[branch].Add(commit);
                            }
                        }
                    }
                    
                }
            }

            #endregion

        }

        public async Task<IReadOnlyList<Octokit.GitHubCommit>> GetCommits(string sha = null, int branchNumber = -1)
        {
            if (!mBranchCommits.ContainsKey(sha))
            {
                CommitRequest cr = new CommitRequest();
                cr.Sha = sha;
                var commitsTask = mGitHubClient.Repository.Commit.GetAll(mOwner, mRepository, cr);
                var commits = await commitsTask;
                mBranchCommits.Add(sha, commits);
            }

            return mBranchCommits[sha];
        }

        public BranchHierarchy CreateHierarchy()
        {
            BranchHierarchy master = new BranchHierarchy() { Name = "master" };
            BranchHierarchy release = new BranchHierarchy { Name = "release", Parent = master };
            BranchHierarchy dev = new BranchHierarchy { Name = "dev", Parent = release };
            BranchHierarchy cherry = new BranchHierarchy { Name = "cherrytest", Parent = release };
            BranchHierarchy featureA = new BranchHierarchy { Name = "featureA", Parent = release };

            master.Children.Add(release);
            release.Children.Add(dev);
            release.Children.Add(cherry);
            release.Children.Add(featureA);

            return master;
        }

        public List<string> GetIndirectTopBranchConnections(BranchHierarchy master, string branchName)
        {
            List<string> branchNames = new List<string>();

            if (!branchName.Equals(master.Name))
            {
                FillBranchNames(master, branchName, branchNames);
            }
            return branchNames;
        }

        public string GetParentBranch(BranchHierarchy parent, string branchName)
        {
            string value = "";
            foreach (var child in parent.Children)
            {
                if (child.Name.Equals(branchName))
                {
                    return value + parent.Name;
                }
                else
                {
                    value += GetParentBranch(child, branchName);
                }
            }

            return value;
        }

        public void FillBranchNames(BranchHierarchy parent, string branchName, List<string> branchNames)
        {
            bool childFound = false;
            foreach (var child in parent.Children)
            {
                if (!child.Name.Equals(branchName))
                {
                    FillBranchNames(child, branchName, branchNames);
                }
                else
                {
                    childFound = true;
                }
            }

            if (!childFound)
            {
                branchNames.Add(parent.Name);
            }
        }

        public BranchHierarchy FindBranchInHierarchy(BranchHierarchy hierarchy, string branchName)
        {
            foreach (var child in hierarchy.Children)
            {
                if (child.Name.Equals(branchName))
                {
                    return child;
                }
                return FindBranchInHierarchy(child, branchName);
            }
            return null;
        }

        public async Task<List<CherryPick>> GetCherryPicks(IReadOnlyList<Octokit.Branch> branches)
        {
            string commentIdentifier = "cherry picked from commit ";
            HashSet<string> cherryPickShas = new HashSet<string>();
            List<CherryPick> cherryPicks = new List<CherryPick>();
            ReadOnlyDictionary<string, GitHubCommit> allCommits = await GetAllCommits(branches);

            List<GitHubCommit> commentedCherryPicks = allCommits.Values.Where(f => f.Commit.Message.Contains(commentIdentifier)).ToList();
            foreach (var cherry in commentedCherryPicks)
            {
                cherryPickShas.Add(cherry.Sha);
                string originalSha = cherry.Commit.Message.Substring(cherry.Commit.Message.IndexOf(commentIdentifier) + commentIdentifier.Length, 40);
                cherryPicks.Add(new CherryPick { OriginalCommit = allCommits[originalSha], CherryPickCommit = cherry });
            }

            return cherryPicks;
        }

        public async Task<ReadOnlyDictionary<string, GitHubCommit>> GetAllCommits(IReadOnlyList<Octokit.Branch> branches)
        {
            Dictionary<string, GitHubCommit> allCommits = new Dictionary<string, GitHubCommit>();

            for (int i = 0; i < branches.Count; i++)
            {
                foreach (var commit in await GetCommits(branches[i].Name))
                {
                    if (!allCommits.ContainsKey(commit.Sha))
                    {
                        allCommits.Add(commit.Sha, commit);
                    }
                }
            }

            return new ReadOnlyDictionary<string, GitHubCommit>(allCommits);
        }
    }
}
