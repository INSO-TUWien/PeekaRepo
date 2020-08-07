using PeekaRepo.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace PeekaRepo.Pages
{
    public partial class BranchVisualization
    {
        [Inject]
        GitApiConnector mConnector { get; set; }
        [Inject]
        JsInteropEChart mJsInteropEChart { get; set; }
        private string ReferenceBranch { get; set; }
        private IReadOnlyList<Octokit.Branch> Branches { get; set; }
        private CompareSummary CompareSummary { get; set; }
        private static Action<string> SelectBranchDetailsAction;
        private string SelectedDetailsBranch { get; set; }
        public Dictionary<string, GitHubCommit> BranchLastCommits { get; set; }
        public List<string> SelectedBranchesToAnalyze { get; set; } = new List<string>();
        Dictionary<string, bool> CheckedBranches { get; set; }
        public string NoConnectionInfo { get; set; }
        protected override async Task OnInitializedAsync()
        {
            if(mConnector.IsConnected)
            {
                Branches = await mConnector.GetBranches();
                
                SelectBranchDetailsAction = SelectBranchDetails;
                BranchLastCommits = mConnector.BranchLastCommits;
                NoConnectionInfo = "";
            }
            else
            {
                NoConnectionInfo = "Please connect to a repository first";
            }
            
        }
        public async Task CheckBoxClicked(string branchName)
        {
            bool newValue = !CheckedBranches[branchName];
            var branchesToDraw = CheckedBranches.Keys.Where(f => CheckedBranches[f]).ToList();
            if (newValue)
            {
                await mConnector.AnalyzeSingleBranch(branchName, CompareSummary);
                branchesToDraw.Add(branchName);
            }
            else
            {
                mConnector.RemoveBranchFromSummary(CompareSummary, branchName);
                branchesToDraw.Remove(branchName);
                if(SelectedDetailsBranch == branchName)
                {
                    SelectedDetailsBranch = ReferenceBranch;
                }
            }
            await mJsInteropEChart.UpdateCharts(branchesToDraw, ReferenceBranch, CompareSummary);
            this.StateHasChanged();
        }

        public async Task ReferenceBranchSelected(ChangeEventArgs e)
        {
            if(!string.IsNullOrEmpty(e.Value.ToString()))
            {
                CheckedBranches = new Dictionary<string, bool>();
                foreach (var branch in Branches)
                {
                    CheckedBranches.Add(branch.Name, false);
                }
                ReferenceBranch = e.Value.ToString();
                SelectedDetailsBranch = ReferenceBranch;
                SelectedBranchesToAnalyze.Clear();
                CompareSummary = await mConnector.AnalyzeReferenceBranch(ReferenceBranch);
               
                foreach (var branch in CompareSummary.Ahead.Keys)
                {
                    SelectedBranchesToAnalyze.Add(branch);
                    CheckedBranches[branch] = true;
                }
      
                await mJsInteropEChart.UpdateCharts(CheckedBranches.Keys.Where(f => CheckedBranches[f]), ReferenceBranch, CompareSummary);

                this.StateHasChanged();
            }
        }

        private void SelectBranchDetails(string branchName)
        {
            foreach(string branch in mJsInteropEChart.LegendData.Keys)
            {
                if(mJsInteropEChart.LegendData[branch] == branchName)
                {
                    SelectedDetailsBranch = branch;
                }
            }
            foreach (string branch in mJsInteropEChart.LegendData2.Keys)
            {
                if (mJsInteropEChart.LegendData2[branch] == branchName)
                {
                    SelectedDetailsBranch = branch;
                }
            }
            this.StateHasChanged();
        }

        [JSInvokable]
        public static void BranchDetailsClicked(string branchName)
        {
            SelectBranchDetailsAction.Invoke(branchName);
        }
    }   
}