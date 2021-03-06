﻿using System;
using System.Collections.Generic;
using PanoramicDataWin8.controller.data;
using PanoramicDataWin8.controller.data.sim;

namespace PanoramicDataWin8.model.data.common
{
    public class Bin
    {
        public Dictionary<InputOperationModel, double> Counts { get; set; }
        public Dictionary<InputOperationModel, double?> Values { get; set; }
        public Dictionary<InputOperationModel, double> Margins { get; set; }
        public Dictionary<InputOperationModel, double> MarginsAbsolute { get; set; }

        public Dictionary<InputOperationModel, double> Means { get; set; }
        public Dictionary<InputOperationModel, double> PowerSumAverage { get; set; }
        public Dictionary<InputOperationModel, double> SampleStandardDeviations { get; set; }
        public Dictionary<InputOperationModel, double> Ns { get; set; }
        public Dictionary<InputOperationModel, object> TemporaryValues { get; set; }
        public Dictionary<InputOperationModel, double?> NormalizedValues { get; set; }
        public List<DataRow> Samples { get; set; }
        public BinIndex BinIndex { get; set; }
        public List<Span> Spans { get; set; }
        public int Count { get; set; }
        public int BrushCount { get; set; }
        
        public Bin()
        {
            Spans = new List<Span>();
            Samples = new List<DataRow>();
            Counts = new Dictionary<InputOperationModel, double>();
            Values = new Dictionary<InputOperationModel, double?>();
            Margins = new Dictionary<InputOperationModel, double>();
            MarginsAbsolute = new Dictionary<InputOperationModel, double>();
            PowerSumAverage = new Dictionary<InputOperationModel, double>();
            Means = new Dictionary<InputOperationModel, double>();
            SampleStandardDeviations = new Dictionary<InputOperationModel, double>();
            Ns = new Dictionary<InputOperationModel, double>();
            TemporaryValues = new Dictionary<InputOperationModel, object>();
            NormalizedValues = new Dictionary<InputOperationModel, double?>();
        }

        public bool ContainsBin(Bin bin)
        {
            for (int d = 0; d < Spans.Count; d++)
            {
                if (!((bin.Spans[d].Min >= this.Spans[d].Min || aboutEqual(bin.Spans[d].Min, this.Spans[d].Min)) &&
                     (bin.Spans[d].Max <= this.Spans[d].Max || aboutEqual(bin.Spans[d].Max, this.Spans[d].Max))))
                {
                    return false;
                }
            }
            return true;
            /*
                return (bin.BinMinX >= this.BinMinX || aboutEqual(bin.BinMinX, this.BinMinX)) &&
                    (bin.BinMaxX <= this.BinMaxX || aboutEqual(bin.BinMaxX, this.BinMaxX)) &&
                    (bin.BinMinY >= this.BinMinY || aboutEqual(bin.BinMinY, this.BinMinY)) &&
                    (bin.BinMaxY <= this.BinMaxY || aboutEqual(bin.BinMaxY, this.BinMaxY));
             */
        }

        public void Map(Bin bin)
        {
            BrushCount = bin.BrushCount;
            BinIndex = bin.BinIndex;
            Spans = bin.Spans;
            Count = bin.Count;

            Counts = bin.Counts;
            Values = bin.Values;
            Margins = bin.Margins;
            MarginsAbsolute = bin.MarginsAbsolute;
            PowerSumAverage = bin.PowerSumAverage;
            Means = bin.Means;
            SampleStandardDeviations = bin.SampleStandardDeviations;
            Ns = bin.Ns;
            TemporaryValues = bin.TemporaryValues;
            NormalizedValues = bin.NormalizedValues;
        }

        private bool aboutEqual(double x, double y)
        {
            double epsilon = Math.Max(Math.Abs(x), Math.Abs(y)) * 1E-15;
            return Math.Abs(x - y) <= epsilon;
        }
    }

    public class Span
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public int Index { get; set; }
    }
}
