using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PeekaRepo.Entities
{
    public class CherryPick
    {
        public string OriginalBranch { get; set; }
        public GitHubCommit OriginalCommit { get; set; }
        public GitHubCommit CherryPickCommit { get; set; }
    }
}
