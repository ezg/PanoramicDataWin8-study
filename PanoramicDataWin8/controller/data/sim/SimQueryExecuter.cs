using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PanoramicDataWin8.controller.data.virt;
using PanoramicDataWin8.controller.view;
using PanoramicDataWin8.model.data;
using PanoramicDataWin8.model.data.result;
using PanoramicDataWin8.model.data.sim;

namespace PanoramicDataWin8.controller.data.sim
{
    public class SimQueryExecuter : QueryExecuter
    {
        public override void ExecuteQuery(QueryModel queryModel)
        {
            queryModel.ResultModel.ResultItemModels = new ObservableCollection<ResultItemModel>();
            queryModel.ResultModel.FireResultModelUpdated(ResultType.Clear);

            if (ActiveJobs.ContainsKey(queryModel))
            {
                ActiveJobs[queryModel].Stop();
                ActiveJobs[queryModel].JobUpdate -= simJob_JobUpdate;
                ActiveJobs[queryModel].JobCompleted -= simJob_JobCompleted;
                ActiveJobs.Remove(queryModel);
            }
            // determine if new job is even needed (i.e., are all relevant inputfieldmodels set)
            if ((queryModel.VisualizationType == VisualizationType.table && queryModel.InputOperationModels.Count > 0) ||
                (queryModel.VisualizationType != VisualizationType.table && queryModel.GetUsageInputOperationModel(InputUsage.X).Any() &&  queryModel.GetUsageInputOperationModel(InputUsage.Y).Any()))
            {
                var queryModelClone = queryModel.Clone();
                SimDataProvider dataProvider = ((SimOriginModel) queryModel.SchemaModel.OriginModels[0]).SimDataProvider;

                DataJob dataJob = new DataJob(
                    queryModel, queryModelClone, dataProvider,
                    MainViewController.Instance.MainModel.BrushQueryModel != queryModel ? MainViewController.Instance.MainModel.BrushQuery : "", MainViewController.Instance.MainModel.FilterQuery);

                ActiveJobs.Add(queryModel, dataJob);
                dataJob.JobUpdate += simJob_JobUpdate;
                dataJob.JobCompleted += simJob_JobCompleted;
                dataJob.Start();
            }
            
        }

        void simJob_JobCompleted(object sender, EventArgs e)
        {
            DataJob dataJob = sender as DataJob;
            dataJob.QueryModel.ResultModel.Progress = 1.0;
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
            dataJob.QueryModel.ResultModel.FireResultModelUpdated(ResultType.Update);
        }
    }
}