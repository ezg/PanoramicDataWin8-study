﻿using Microsoft.Practices.Prism.Mvvm;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PanoramicDataWin8.model.data.sim;

namespace PanoramicDataWin8.model.data
{
    [JsonObject(MemberSerialization.OptOut)]
    public class InputOperationModel : BindableBase
    {
        public InputOperationModel(InputModel inputModel)
        {
            _inputModel = inputModel;
        }

        private InputModel _inputModel = null;

        public InputModel InputModel
        {
            get
            {
                return _inputModel;
            }
            set
            {
                this.SetProperty(ref _inputModel, value);
            }
        }

        private QueryModel _queryModel = null;
        [JsonIgnore]
        public QueryModel QueryModel
        {
            get
            {
                return _queryModel;
            }
            set
            {
                this.SetProperty(ref _queryModel, value);
            }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        private AggregateFunction _aggregateFunction = AggregateFunction.None;
        public AggregateFunction AggregateFunction
        {
            get
            {
                return _aggregateFunction;
            }
            set
            {
                this.SetProperty(ref _aggregateFunction, value);
            }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        private TransformationFunction _transformationFunction = TransformationFunction.None;
        public TransformationFunction TransformationFunction
        {
            get
            {
                return _transformationFunction;
            }
            set
            {
                this.SetProperty(ref _transformationFunction, value);
            }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        private SortMode _sortMode = SortMode.None;
        public SortMode SortMode
        {
            get
            {
                return _sortMode;
            }
            set
            {
                this.SetProperty(ref _sortMode, value);
            }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        private ScaleFunction _scaleFunction = ScaleFunction.None;
        public ScaleFunction ScaleFunction
        {
            get
            {
                return _scaleFunction;
            }
            set
            {
                this.SetProperty(ref _scaleFunction, value);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is InputOperationModel)
            {
                var aom = obj as InputOperationModel;
                return
                    aom._aggregateFunction.Equals(this.AggregateFunction) &&
                    aom._inputModel.Equals(this._inputModel) &&
                    aom._transformationFunction.Equals(this._transformationFunction) &&
                    aom._scaleFunction.Equals(this._scaleFunction) &&
                    aom._sortMode.Equals(this._sortMode);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int code = 0;
            code ^= this._aggregateFunction.GetHashCode();
            code ^= this._inputModel.GetHashCode();
            code ^= this._transformationFunction.GetHashCode();
            code ^= this._scaleFunction.GetHashCode();
            //code ^= this._sortMode.GetHashCode();
            return code;
        }

        public override string ToString()
        {
            if (InputModel is SimInputFieldModel)
            {
                return InputModel.ToString();
            }
            return base.ToString();
        }
    }

    public enum AggregateFunction { None, Sum, Count, Min, Max, Avg };

    public enum SortMode { Asc, Desc, None }

    public enum TransformationFunction { None, Year, MonthOfTheYear, DayOfTheMonth, DayOfTheWeek, HourOfTheDay}

    public enum ScaleFunction { None, Log, Normalize, RunningTotal, RunningTotalNormalized };
}
