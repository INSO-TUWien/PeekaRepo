using PeekaRepo.Entities;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PeekaRepo.Entities
{
    public class JsInteropEChart
    {
        private readonly IJSRuntime mJsRuntime;
        public Dictionary<string, string> LegendData = new Dictionary<string, string>();
        public Dictionary<string, string> LegendData2 = new Dictionary<string, string>();

        public JsInteropEChart(IJSRuntime jsRuntime)
        {
            this.mJsRuntime = jsRuntime;
        }

        public async Task UpdateCharts(IEnumerable<string> branchesToDraw, string referenceBranch, CompareSummary compareSummary)
        {
            List<DataItem> aheadData = new List<DataItem>();
            List<DataItem> behindData = new List<DataItem>();
            List<DataItem> conflictData = new List<DataItem>();

            LegendData = new Dictionary<string, string>();
            LegendData2 = new Dictionary<string, string>();
            LegendData.Add(referenceBranch, referenceBranch);
            LegendData2.Add(referenceBranch, referenceBranch + " (" + compareSummary.IndirectConflicts.Count + ")");
         
            foreach (var branch in branchesToDraw)
            {
                LegendData.Add(branch, branch + " (+" + compareSummary.Ahead[branch].AheadBy + "/-" + compareSummary.Behind[branch].AheadBy + ")");
                LegendData2.Add(branch, branch + " (" + compareSummary.FileConflicts[branch].Count + ")");
            }
            
            foreach (var branch in branchesToDraw)
            {
                aheadData.Add(
                    new DataItem 
                    { 
                        name = LegendData[branch], 
                        value = compareSummary.Ahead[branch].AheadBy
                    });
            }
            
            foreach (var branch in branchesToDraw)
            {
                behindData.Add(
                    new DataItem
                    { 
                        name = LegendData[branch], 
                        value = compareSummary.Behind[branch].AheadBy
                    });
            }

            foreach (var branch in branchesToDraw)
            {
                conflictData.Add(
                    new DataItem 
                    { 
                        name = LegendData2[branch], 
                        value = compareSummary.FileConflicts[branch].Count, 
                        tooltip = 
                            new Tooltip 
                            { 
                                formatter = "{c} {a} with " + referenceBranch 
                            } 
                    });
            }
            conflictData.Add(
                new DataItem 
                { 
                    name = LegendData2[referenceBranch], 
                    value = compareSummary.IndirectConflicts.Count, 
                    tooltip = new Tooltip 
                    { 
                        formatter = "{c} indirect {a}" 
                    } 
                });

            await mJsRuntime.InvokeVoidAsync("initCharts");
            await mJsRuntime.InvokeVoidAsync("updateCommitChart", referenceBranch, LegendData.Values.ToList(), aheadData, behindData);
            await mJsRuntime.InvokeVoidAsync("updateConflictChart", LegendData2.Values.ToList(), conflictData);
        }

        private class DataItem
        {
            public int value { get; set; }
            public string name { get; set; }
            public Tooltip tooltip { get; set; }
        }
        private class Tooltip
        {
            public string formatter { get; set; }
        }
    }
}
