using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using DynamicExpresso;
using PanoramicDataWin8.controller.data.tuppleware;
using PanoramicDataWin8.controller.view;
using PanoramicDataWin8.model.data;
using PanoramicDataWin8.model.data.common;
using PanoramicDataWin8.model.data.result;
using PanoramicDataWin8.model.data.sim;
using PanoramicDataWin8.model.view;

namespace PanoramicDataWin8.controller.data
{
    public class DataJob : Job
    {
        private DataProvider _dataProvider = null;
        private bool _isRunning = false;
        private bool _isIncremental = false;
        private DataBinner _binner = null;
        private DataAggregator _aggregator = new DataAggregator();
        private Object _lock = new Object();
        private List<AxisType> _axisTypes = new List<AxisType>();
        private Stopwatch _stopWatch = new Stopwatch();

        private List<InputOperationModel> _dimensions = new List<InputOperationModel>();
        private List<Dictionary<string, double>> _uniqueValues = new List<Dictionary<string, double>>();

        public QueryModel QueryModel { get; set; }
        public QueryModel QueryModelClone { get; set; }
        public string BrushQuery { get; set; }
        public string FilterQuery { get; set; }

        public DataJob(QueryModel queryModel, QueryModel queryModelClone, DataProvider dataProvider, string brushQuery, string filterQuery)
        {
            QueryModel = queryModel;
            QueryModelClone = queryModelClone;

            BrushQuery = brushQuery;
            FilterQuery = filterQuery;

            _dataProvider = dataProvider;
        }

        public override void Start()
        {
            _stopWatch.Start();

            _isRunning = true;

            if (QueryModel.VisualizationType == VisualizationType.table)
            {
                _binner = null;
                _dimensions = QueryModelClone.GetUsageInputOperationModel(InputUsage.X).Concat(
                                 QueryModelClone.GetUsageInputOperationModel(InputUsage.Y)).Concat(
                                 QueryModelClone.GetUsageInputOperationModel(InputUsage.Group)).ToList();
            }
            else
            {
                _dimensions = QueryModelClone.GetUsageInputOperationModel(InputUsage.X).Concat(
                                 QueryModelClone.GetUsageInputOperationModel(InputUsage.Y)).Concat(
                                 QueryModelClone.GetUsageInputOperationModel(InputUsage.Group)).ToList();

                _uniqueValues = _dimensions.Select(d => new Dictionary<string, double>()).ToList();

                _axisTypes = _dimensions.Select(d => QueryModelClone.GetAxisType(d)).ToList();
                QueryModel.ResultModel.ResultDescriptionModel = new VisualizationResultDescriptionModel();
                (QueryModel.ResultModel.ResultDescriptionModel as VisualizationResultDescriptionModel).AxisTypes = _axisTypes;

                _isIncremental = _dimensions.Any(aom => aom.AggregateFunction == AggregateFunction.None);

                _binner = new DataBinner()
                {
                    NrOfBins = new double[] { MainViewController.Instance.MainModel.NrOfXBins, MainViewController.Instance.MainModel.NrOfYBins }.Concat(
                                    QueryModel.GetUsageInputOperationModel(InputUsage.Group).Select(qom => MainViewController.Instance.MainModel.NrOfGroupBins)).ToList(),
                    Incremental = _isIncremental,
                    AxisTypes = _axisTypes,
                    IsAxisAggregated = _dimensions.Select(d => d.AggregateFunction != AggregateFunction.None).ToList(),
                    Dimensions = _dimensions.ToList()
                };
            }
            Task.Run(() => run());

            //ThreadPool.RunAsync(_ => run(), WorkItemPriority.Low);
        }

        public override void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;
            }
        }

        private async void run()
        {
            try
            {
                if (!_dataProvider.IsInitialized)
                {
                    _dataProvider.StartSampling();
                }

                int sampleSize = 0;
                TimeSpan throttle = TimeSpan.FromMilliseconds(0);
                if (MainViewController.Instance.MainModel.Mode == Mode.instantaneous)
                {
                    sampleSize = 10000;
                }
                else if (MainViewController.Instance.MainModel.Mode == Mode.batch)
                {
                    sampleSize = 10000;
                    throttle = TimeSpan.FromMilliseconds(5000);
                }
                else if (MainViewController.Instance.MainModel.Mode == Mode.progressive)
                {
                    sampleSize = 1000;
                    throttle = TimeSpan.FromMilliseconds(500);
                }
                int from = 0;
                DataPage dataPage = _dataProvider.GetSampleDataRows(from, sampleSize);

                List<ResultItemModel> resultItemModels = new List<ResultItemModel>();
                while (dataPage != null && _isRunning && from < _dataProvider.GetNrTotalSamples())
                {
                    if (throttle.Ticks > 0)
                    {
                        await Task.Delay(throttle);
                    }
                    from += sampleSize;
                    double progress = Math.Min(1.0, (double)from / (double)_dataProvider.GetNrTotalSamples());
                    filterDataPage(dataPage);

                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    if (QueryModelClone.VisualizationType != VisualizationType.table)
                    {
                        if (!_isIncremental)
                        {
                            _uniqueValues = _dimensions.Select(d => new Dictionary<string, double>()).ToList();
                        }
                        setVisualizationValues(dataPage.DataRows);
                        if (_binner != null)
                        {
                            _binner.BinStep(dataPage.DataRows);
                        }
                        if (_aggregator != null)
                        {
                            _aggregator.AggregateStep(_binner.BinStructure, QueryModel, QueryModelClone, progress, BrushQuery);
                        }

                        ResultDescriptionModel resultDescriptionModel = null;
                        if (_binner != null && _binner.BinStructure != null)
                        {
                            resultItemModels = convertBinsToResultItemModels(_binner.BinStructure);
                            resultDescriptionModel = new VisualizationResultDescriptionModel()
                            {
                                BinRanges = _binner.BinStructure.BinRanges,
                                NullCount = _binner.BinStructure.NullCount,
                                Dimensions = _dimensions,
                                AxisTypes = _axisTypes,
                                MinValues = _binner.BinStructure.AggregatedMinValues.ToDictionary(entry => entry.Key, entry => entry.Value),
                                MaxValues = _binner.BinStructure.AggregatedMaxValues.ToDictionary(entry => entry.Key, entry => entry.Value)
                            };

                            await fireUpdated(resultItemModels, progress, resultDescriptionModel);
                        }
                    }
                    if (MainViewController.Instance.MainModel.Verbose)
                    {
                        Debug.WriteLine("DataJob Iteration Time: " + sw.ElapsedMilliseconds);
                    }
                    dataPage = _dataProvider.GetSampleDataRows(from, sampleSize);
                }

                if (_isRunning)
                {
                    await fireCompleted();
                }
                lock (_lock)
                {
                    _isRunning = false;
                }
            }
            catch (Exception exc)
            {
                ErrorHandler.HandleError(exc.Message);
            }
        }

        private void filterDataPage(DataPage dataPage)
        {
            if (FilterQuery != "")
            {
                var interpreter = new Interpreter(); var originModel = ((SimSchemaModel)QueryModel.SchemaModel).RootOriginModel;
                List<Parameter> parameters = originModel.CreateParameters();
                
                Lambda parsedExpression = interpreter.Parse(FilterQuery, parameters.ToArray());
                List<DataRow> filteredRows = new List<DataRow>();
                foreach (var row in dataPage.DataRows)
                {
                    List<object> rowValues = row.CreateRowValues(QueryModel);
                    var result = parsedExpression.Invoke(rowValues.ToArray());
                    if (result is bool && (bool)result)
                    {
                        filteredRows.Add(row);
                    }
                }
                dataPage.DataRows = filteredRows;
            }
        }

        private void setVisualizationValues(List<DataRow> samples)
        {
            foreach (var sample in samples)
            {
                for (int d = 0; d < _dimensions.Count; d++)
                {
                    sample.VisualizationValues[_dimensions[d]] = getVisualizationValue(_axisTypes[d], sample.Entries[_dimensions[d].InputModel as InputFieldModel], _dimensions[d], _uniqueValues[d]);
                }
            }
        }

        private double? getVisualizationValue(AxisType axisType, object value, InputOperationModel inputOperationModel, Dictionary<string, double> uniqueValues) 
        {
            if (((InputFieldModel) inputOperationModel.InputModel).InputVisualizationType == InputVisualizationTypeConstants.ENUM ||
                ((InputFieldModel) inputOperationModel.InputModel).InputVisualizationType == InputVisualizationTypeConstants.CATEGORY)
            {
                if (value != null)
                {
                    if (!uniqueValues.ContainsKey(value.ToString()))
                    {
                        uniqueValues.Add(value.ToString(), uniqueValues.Count);
                    }
                    return uniqueValues[value.ToString()];
                }
            }
            else if (((InputFieldModel) inputOperationModel.InputModel).InputDataType == InputDataTypeConstants.FLOAT ||
                ((InputFieldModel) inputOperationModel.InputModel).InputDataType == InputDataTypeConstants.INT)
            {
                return value == null ? null : (double?)double.Parse(value.ToString());
            }
            else if (((InputFieldModel)inputOperationModel.InputModel).InputDataType == InputDataTypeConstants.TIME)
            {
                return value == null ? null : (double?)((DateTime)value).TimeOfDay.Ticks;
            }
            else if (((InputFieldModel)inputOperationModel.InputModel).InputDataType == InputDataTypeConstants.DATE)
            {
                return value == null ? null : (double?)((DateTime)value).Ticks;
            }
            return null;
        }

        private List<ResultItemModel> convertBinsToResultItemModels(BinStructure binStructure)
        {
            List<ResultItemModel> returnValues = new List<ResultItemModel>();

            for (int d = 0; d < _dimensions.Count; d++)
            {
                if (binStructure.BinRanges[d] is NominalBinRange)
                {
                    (binStructure.BinRanges[d] as NominalBinRange).SetLabels(_uniqueValues[d]);
                }
            }
            foreach (var bin in binStructure.Bins.Values)
            {
                VisualizationItemResultModel itemModel = new VisualizationItemResultModel();
                itemModel.BrushCount = bin.BrushCount;
                itemModel.Count = bin.Count;
                for (int d = 0; d < _dimensions.Count; d++)
                {
                    if (!(binStructure.BinRanges[d] is AggregateBinRange))
                    {
                        itemModel.AddValue(_dimensions[d],
                            new ResultItemValueModel(bin.Spans[d].Min, bin.Spans[d].Max));
                    }
                }

                foreach (var aom in bin.Values.Keys)
                {
                    itemModel.AddValue(aom, new ResultItemValueModel(
                               bin.Values[aom],
                               bin.NormalizedValues[aom]));
                }      
                returnValues.Add(itemModel);
            }

            return returnValues;
        }


        private async Task fireUpdated(List<ResultItemModel> samples, double progress, ResultDescriptionModel resultDescriptionModel)
        {
            var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                FireJobUpdated(new JobEventArgs()
                {
                    Samples = samples,
                    Progress = progress,
                    ResultDescriptionModel = resultDescriptionModel
                });
            });
        }

        private async Task fireCompleted()
        {
            var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                FireJobCompleted(new EventArgs());
            }); 
            if (MainViewController.Instance.MainModel.Verbose)
            {
                Debug.WriteLine("DataJob Total Run Time: " + _stopWatch.ElapsedMilliseconds);
            }
        }
    }

    public class JobEventArgs : EventArgs
    {
        public List<ResultItemModel> Samples { get; set; }
        public double Progress { get; set; }
        public ResultDescriptionModel ResultDescriptionModel { get; set; }
    }
}

