using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PeekaRepo.Entities
{
    public class CompareSummary
    {
        public string BranchName { get; set; }
        public Dictionary<string, CompareResult> Ahead { get; set; } = new Dictionary<string, CompareResult>();
        public Dictionary<string, CompareResult> Behind { get; set; } = new Dictionary<string, CompareResult>();
        public Dictionary<string, List<string>> FileConflicts { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> IndirectConflicts { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, List<GitHubCommit>> WrongMergedCommits = new Dictionary<string, List<GitHubCommit>>();
    }
}
