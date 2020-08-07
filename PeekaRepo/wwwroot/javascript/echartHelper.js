var echart = null;
var radarChart = null;

function initCharts() {
    echart = null;
    radarChart = null;
    if (echart == null) {
        echart = echarts.init(document.getElementById('commitChart'));
        echart.on("click", params => {
            DotNet.invokeMethodAsync("PeekaRepo", "BranchDetailsClicked", params.data.name);
        });
    }
    if (radarChart == null) {
        radarChart = echarts.init(document.getElementById('conflictChart'));
        radarChart.on("click", params => {
            DotNet.invokeMethodAsync("PeekaRepo", "BranchDetailsClicked", params.data.name);
        });
    }
}


function updateConflictChart(legendData, conflictData) {
    var option = {
        title: {
            text: 'Conflict states',
            left: 'center'
        },
        tooltip: {
            trigger: 'item',
            formatter: '{c} {a}'
        },
        legend: {
            left: 'center',
            top: 'bottom',
            data: legendData
        },
        series: [

            {
                name: 'conflict(s)',
                type: 'pie',
                radius: [20, 230],
                center: ['50%', '50%'],
                roseType: 'area',
                selectMode: 'single',
                selectedOffset: 20,
                data: conflictData
            }
        ]
    };
    radarChart.setOption(option);
}

function updateCommitChart(branchName,legendData, aheadData, behindData) {
    var option = {
        title: {
            text: 'Commit States',
            left: 'center'
        },
        tooltip: {
            trigger: 'item',
            formatter: '{c} commit(s) {a} of ' + branchName
        },
        legend: {
            left: 'center',
            top: 'bottom',
            data: legendData
        },
        series: [
            
            {
                name: 'Ahead',
                type: 'pie',
                radius: [130, 230],
                center: ['50%', '50%'],
                roseType: 'area',
                selectMode: 'single',
                label: {
                    show: true
                },
                emphasis: {
                    label: {
                        show: true
                    }
                },
                data: aheadData
            },
            {
                name: 'Behind',
                type: 'pie',
                radius: [110, 10],
                label: {
                    show: false
                },
                center: ['50%', '50%'],
                roseType: 'area',
                selectMode: 'single',
                data: behindData
            },
            {
                name: branchName,
                type: 'pie',
                radius: [110, 130],
                label: {
                    show: true
                },
                center: ['50%', '50%'],
                roseType: 'area',
                selectMode: 'single',
                data: [
                    { value: 10, name: branchName, tooltip: {formatter: '{a}'} }

                ]
            }
        ]
    };
    echart.setOption(option);
}