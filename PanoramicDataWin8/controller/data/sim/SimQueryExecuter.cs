using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using PanoramicDataWin8.controller.data.virt;
using PanoramicDataWin8.controller.view;
using PanoramicDataWin8.model.data;
using PanoramicDataWin8.model.data.result;
using PanoramicDataWin8.model.data.sim;

namespace PanoramicDataWin8.controller.data.sim
{
    public class SimQueryExecuter : QueryExecuter
    {
        public delegate void ExecuteQueryHandler(object sender, ExecuteQueryEventArgs e);
        public event ExecuteQueryHandler ExecuteQuery;

        public SimQueryExecuter()
        {
            var stream = Observable.FromEventPattern<ExecuteQueryEventArgs>(this, "ExecuteQuery");
            stream.GroupByUntil(k => k.EventArgs.QueryModel, g => Observable.Timer(TimeSpan.FromMilliseconds(5)))
                .SelectMany(y => y.FirstAsync())
                .Subscribe((async (arg) =>
                {
                    var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        var queryModel = arg.EventArgs.QueryModel;
                        queryModel.ResultModel.ResultItemModels = new ObservableCollection<ResultItemModel>();
                        queryModel.ResultModel.Progress = 0.0;
                        queryModel.ResultModel.FireResultModelUpdated(ResultType.Clear);

                        if (ActiveJobs.ContainsKey(queryModel))
                        {
                            ActiveJobs[queryModel].Stop();
                            ActiveJobs[queryModel].JobUpdate -= simJob_JobUpdate;
                            ActiveJobs[queryModel].JobCompleted -= simJob_JobCompleted;
                            ActiveJobs[queryModel].JobStopped -= simJob_JobStopped;
                            ActiveJobs.Remove(queryModel);
                        }
                        // determine if new job is even needed (i.e., are all relevant inputfieldmodels set)
                        if ((queryModel.VisualizationType == VisualizationType.table && queryModel.InputOperationModels.Count > 0) ||
                            (queryModel.VisualizationType != VisualizationType.table && queryModel.GetUsageInputOperationModel(InputUsage.X).Any() && queryModel.GetUsageInputOperationModel(InputUsage.Y).Any()))
                        {
                            var queryModelClone = queryModel.Clone();
                            SimDataProvider dataProvider = ((SimOriginModel)queryModel.SchemaModel.OriginModels[0]).SimDataProvider;

                            DataJob dataJob = new DataJob(
                                queryModel, queryModelClone, dataProvider,
                                MainViewController.Instance.MainModel.BrushQueryModel != queryModel ? MainViewController.Instance.MainModel.BrushQuery : "", MainViewController.Instance.MainModel.FilterQuery);

                            ActiveJobs.Add(queryModel, dataJob);
                            dataJob.JobUpdate += simJob_JobUpdate;
                            dataJob.JobCompleted += simJob_JobCompleted;
                            dataJob.JobStopped += simJob_JobStopped;
                            dataJob.Start();
                        }
                    });
                }));
        }

        public override void FireExecuteQuery(QueryModel queryModel)
        {
            if (ExecuteQuery != null)
            {
                ExecuteQuery(this, new ExecuteQueryEventArgs(queryModel));
            }
        }

        void simJob_JobCompleted(object sender, JobEventArgs jobEventArgs)
        {
            DataJob dataJob = sender as DataJob;

            var oldItems = dataJob.QueryModel.ResultModel.ResultItemModels;
            oldItems.Clear();
            foreach (var sample in jobEventArgs.Samples)
            {
                oldItems.Add(sample);
            }

            dataJob.QueryModel.ResultModel.Progress = 1.0;
            dataJob.QueryModel.ResultModel.ResultDescriptionModel = jobEventArgs.ResultDescriptionModel;

            Logger.Instance.LogQueryResult("complete", dataJob.QueryModel, dataJob.QueryModel.ResultModel, jobEventArgs.BrushQuery, jobEventArgs.FilterQuery);
            dataJob.QueryModel.ResultModel.FireResultModelUpdated(ResultType.Complete);
        }

        void simJob_JobUpdate(object sender, JobEventArgs jobEventArgs)
        {
            DataJob dataJob = sender as DataJob;

            var oldItems = dataJob.QueryModel.ResultModel.ResultItemModels;
            oldItems.Clear();
            foreach (var sample in jobEventArgs.Samples)
            {
                oldItems.Add(sample);
            }
            
            dataJob.QueryModel.ResultModel.Progress = jobEventArgs.Progress;
            dataJob.QueryModel.ResultModel.ResultDescriptionModel = jobEventArgs.ResultDescriptionModel;

            Logger.Instance.LogQueryResult("update", dataJob.QueryModel, dataJob.QueryModel.ResultModel, jobEventArgs.BrushQuery, jobEventArgs.FilterQuery);
            dataJob.QueryModel.ResultModel.FireResultModelUpdated(ResultType.Update);
        }
        
        private void simJob_JobStopped(object sender, JobEventArgs jobEventArgs)
        {
            DataJob dataJob = sender as DataJob;
            Logger.Instance.LogQueryResult("stopped", dataJob.QueryModel, dataJob.QueryModel.ResultModel, jobEventArgs.BrushQuery, jobEventArgs.FilterQuery);
        }

    }

    public class ExecuteQueryEventArgs : EventArgs
    {
        public QueryModel QueryModel { get; set; }

        public ExecuteQueryEventArgs(QueryModel queryModel)
            : base()
        {
            this.QueryModel = queryModel;
        }
    }

}