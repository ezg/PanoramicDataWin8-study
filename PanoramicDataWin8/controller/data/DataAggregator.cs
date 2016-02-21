using System;
using System.Collections.Generic;
using System.Linq;
using DynamicExpresso;
using PanoramicDataWin8.model.data;
using PanoramicDataWin8.model.data.common;
using PanoramicDataWin8.model.data.sim;

namespace PanoramicDataWin8.controller.data
{
    public class DataAggregator
    {
        public void AggregateStep(BinStructure binStructure, QueryModel queryModel, QueryModel queryModelClone, double progress, string brushQuery)
        {
            if (binStructure == null)
            {
                return;
            }

            double maxCount = double.MinValue;
            double sumCount = 0;
            double z95 = 1.96;
            binStructure.AggregatedMaxValues.Clear();
            binStructure.AggregatedMinValues.Clear();

            var aggregates = queryModelClone.GetUsageInputOperationModel(InputUsage.Value).Concat(
                             queryModelClone.GetUsageInputOperationModel(InputUsage.DefaultValue)).Concat(
                             queryModelClone.GetUsageInputOperationModel(InputUsage.X).Where(aom => aom.AggregateFunction != AggregateFunction.None)).Concat(
                             queryModelClone.GetUsageInputOperationModel(InputUsage.Y).Where(aom => aom.AggregateFunction != AggregateFunction.None)).Distinct().ToList();

            Lambda brushParsedExpression = null;
            if (brushQuery != "")
            {
                var interpreter = new Interpreter();
                var originModel = ((SimSchemaModel)queryModel.SchemaModel).RootOriginModel;
                List<Parameter> parameters = originModel.CreateParameters();
                brushParsedExpression = interpreter.Parse(brushQuery, parameters.ToArray());
            }

            // update aggregations and counts
            foreach (var bin in binStructure.Bins.Values)
            {
                bin.Count += bin.Samples.Count;
                maxCount = Math.Max(bin.Count, maxCount);
                foreach (var sample in bin.Samples)
                {
                    foreach (var aggregator in aggregates)
                    {
                        update(bin, sample, aggregator);
                    }

                    if (brushParsedExpression != null)
                    {
                        var result = brushParsedExpression.Invoke(sample.CreateRowValues(queryModel).ToArray());
                        if (result is bool && (bool) result)
                        {
                            bin.BrushCount++;
                        }
                    }
                }
            }

            // interpolate if needed
            foreach (var bin in binStructure.Bins.Values)
            {
                if (aggregates.Any())
                {
                    foreach (var aggregator in aggregates)
                    {

                        if (aggregator.AggregateFunction == AggregateFunction.Count && bin.Values.ContainsKey(aggregator) && bin.TemporaryValues.ContainsKey(aggregator))
                        {
                            bin.Values[aggregator] = progress < 1.0 ? (double) bin.TemporaryValues[aggregator]/progress : (double) bin.TemporaryValues[aggregator];
                        }
                    }
                }
            }


            // update max / min 
            foreach (var aggregator in aggregates)
            {
                if (!binStructure.AggregatedMaxValues.ContainsKey(aggregator))
                {
                    binStructure.AggregatedMaxValues.Add(aggregator, double.MinValue);
                    binStructure.AggregatedMinValues.Add(aggregator, double.MaxValue);
                }
                binStructure.AggregatedMaxValues[aggregator] = double.MinValue;
                binStructure.AggregatedMinValues[aggregator] = double.MaxValue;

                foreach (var bin in binStructure.Bins.Values)
                {
                    if (bin.Values.ContainsKey(aggregator) && bin.Values[aggregator].HasValue)
                    {
                        binStructure.AggregatedMaxValues[aggregator] = Math.Max(binStructure.AggregatedMaxValues[aggregator], bin.Values[aggregator].Value);
                        binStructure.AggregatedMinValues[aggregator] = Math.Min(binStructure.AggregatedMinValues[aggregator], bin.Values[aggregator].Value);
                    }
                }
            }

            // update margins (errors)
            sumCount = binStructure.Bins.Values.Sum(b => b.Count);
            
            foreach (var aggregator in aggregates)
            {
                if (aggregator.AggregateFunction == AggregateFunction.Count)
                {
                    foreach (var bin in binStructure.Bins.Values)
                    {
                        var totalCountInterpolated = ((double)bin.Count / progress);
                        double fpc = Math.Sqrt((totalCountInterpolated - bin.Count)/(totalCountInterpolated - 1));
                        if (!bin.Margins.ContainsKey(aggregator))
                        {
                            bin.Margins.Add(aggregator, 0);
                            bin.MarginsAbsolute.Add(aggregator, 0);
                        }
                        var probability = ((double) bin.Count) / sumCount;
                        var margin = ((probability*(1.0 - probability))/ sumCount) * fpc;
                        margin = Math.Sqrt(margin) * z95;
                        if (!double.IsNaN(margin) && !double.IsNaN(totalCountInterpolated))
                        {
                            bin.MarginsAbsolute[aggregator] = margin*totalCountInterpolated;
                            bin.Margins[aggregator] = margin;
                        }
                    }
                }
                else if (aggregator.AggregateFunction == AggregateFunction.Avg)
                {
                    foreach (var bin in binStructure.Bins.Values)
                    {
                        if (!bin.Means.ContainsKey(aggregator))
                        {
                            bin.Means.Add(aggregator, 0);
                        }
                        if (!bin.PowerSumAverage.ContainsKey(aggregator))
                        {
                            bin.PowerSumAverage.Add(aggregator, 0);
                        }
                        if (!bin.SampleStandardDeviations.ContainsKey(aggregator))
                        {
                            bin.SampleStandardDeviations.Add(aggregator, 0);
                        }
                        if (!bin.Ns.ContainsKey(aggregator))
                        {
                            bin.Ns.Add(aggregator, 0);
                        }
                        foreach (var dataRow in bin.Samples)
                        {
                            double x = 0;
                            if (double.TryParse(dataRow.Entries[aggregator.InputModel as InputFieldModel].ToString(), out x))
                            {
                                double n = bin.Ns[aggregator] + 1;
                                bin.Ns[aggregator] = n;
                                bin.Means[aggregator] = bin.Means[aggregator] + (x - bin.Means[aggregator])/n;
                                bin.PowerSumAverage[aggregator] = bin.PowerSumAverage[aggregator] + (x*x - bin.PowerSumAverage[aggregator])/n;
                                
                                if (bin.Ns[aggregator] - 1 > 0)
                                {
                                    double mean = bin.Means[aggregator];
                                    bin.SampleStandardDeviations[aggregator] = Math.Sqrt((bin.PowerSumAverage[aggregator]*n - n*mean*mean)/(n - 1));
                                }
                            }
                        }
                        if (!bin.Margins.ContainsKey(aggregator))
                        {
                            bin.Margins.Add(aggregator, 0);
                            bin.MarginsAbsolute.Add(aggregator, 0);
                        }

                        var totalCountInterpolated = ((double)bin.Count / progress);
                        double fpc = Math.Sqrt((totalCountInterpolated - bin.Count) / (totalCountInterpolated - 1));

                        bin.Margins[aggregator] = ((bin.SampleStandardDeviations[aggregator] / Math.Sqrt((double)bin.Count)) * fpc)*z95;
                        bin.MarginsAbsolute[aggregator] = ((bin.SampleStandardDeviations[aggregator] / Math.Sqrt((double)bin.Count)) * fpc) * z95;
                    }
                }
            }

            // normalized values
            foreach (var bin in binStructure.Bins.Values)
            {
                if (aggregates.Any())
                {
                    foreach (var aggregator in bin.Values.Keys)
                    {
                        if (binStructure.AggregatedMaxValues.ContainsKey(aggregator) && binStructure.AggregatedMinValues.ContainsKey(aggregator) &&
                            (binStructure.AggregatedMaxValues[aggregator] - binStructure.AggregatedMinValues[aggregator]) != 0.0)
                        {
                            bin.NormalizedValues[aggregator] =
                                (bin.Values[aggregator] - binStructure.AggregatedMinValues[aggregator]) / (binStructure.AggregatedMaxValues[aggregator] - binStructure.AggregatedMinValues[aggregator]);
                        }
                        else
                        {
                            bin.NormalizedValues[aggregator] = 1.0;
                        }
                    }
                }
            }
        }

        private void update(Bin bin, DataRow sample, InputOperationModel aggregator)
        {
            double? currentValue = null;
            double? sampleValue = null;
            object currentTempValue = null;

            double d = 0;
            if (aggregator.AggregateFunction == AggregateFunction.Count) 
            {
                sampleValue = 0;
                currentTempValue = 0d;
            }
            else if (double.TryParse(sample.Entries[aggregator.InputModel as InputFieldModel].ToString(), out d))
            {
                sampleValue = d;
            }

            if (bin.Values.ContainsKey(aggregator))
            {
                currentValue = bin.Values[aggregator];
            }
            else
            {
                currentValue = sampleValue;
            }

            if (bin.TemporaryValues.ContainsKey(aggregator))
            {
                currentTempValue = bin.TemporaryValues[aggregator];
            }

            if (!bin.Counts.ContainsKey(aggregator)) 
            {
                bin.Counts.Add(aggregator, 0);
            }

            if (sampleValue.HasValue)
            {
                if (aggregator.AggregateFunction == AggregateFunction.Max)
                {
                    currentValue = new double?[] { currentValue, sampleValue }.Max();
                }
                else if (aggregator.AggregateFunction == AggregateFunction.Min)
                {
                    currentValue = new double?[] { currentValue, sampleValue }.Min();
                }
                else if (aggregator.AggregateFunction == AggregateFunction.Avg)
                {
                    currentValue = ((currentValue * bin.Counts[aggregator]) + sampleValue) / (bin.Counts[aggregator] + 1);
                }
                else if (aggregator.AggregateFunction == AggregateFunction.Sum)
                {
                    currentValue = currentValue + sampleValue;
                }
                else if (aggregator.AggregateFunction == AggregateFunction.Count)
                {
                    currentTempValue = (double)currentTempValue + 1;
                    currentValue = (double) currentTempValue;
                }
                else
                {
                    currentValue = ((currentValue * bin.Counts[aggregator]) + sampleValue) / (bin.Counts[aggregator] + 1);
                    //currentValue = currentValue + 1;
                }
            }

            if (!bin.Values.ContainsKey(aggregator))
            {
                bin.Values.Add(aggregator, 0);
                bin.TemporaryValues.Add(aggregator, null);
                bin.NormalizedValues.Add(aggregator, 0);
            } 

            bin.Values[aggregator] = currentValue;
            bin.TemporaryValues[aggregator] = currentTempValue;
            bin.Counts[aggregator] += 1;
        }
    }
}
