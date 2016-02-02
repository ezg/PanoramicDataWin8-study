using System;
using System.Collections.Generic;
using System.Linq;
using DynamicExpresso;
using Newtonsoft.Json;
using PanoramicDataWin8.controller.data.sim;
using PanoramicDataWin8.controller.input;

namespace PanoramicDataWin8.model.data.sim
{
    [JsonObject(MemberSerialization.OptOut)]
    public class SimOriginModel : OriginModel
    {
        public SimOriginModel(DatasetConfiguration datasetConfiguration)
        {
            _datasetConfiguration = datasetConfiguration;
        }

        public void LoadInputFields()
        {
            for (int i = 0; i < _datasetConfiguration.InputFieldNames.Count; i++)
            {
                InputFieldModel inputModel = new SimInputFieldModel(
                    _datasetConfiguration.InputFieldNames[i],
                    _datasetConfiguration.InputFieldDataTypes[i],
                    _datasetConfiguration.InputFieldVisualizationTypes[i]);
                inputModel.OriginModel = this;
                _inputModels.Add(inputModel);
            }

            for (int i = 0; i < _inputModels.Count; i++)
            {
                if (_datasetConfiguration.InputFieldIsDisplayed.Count > i && !_datasetConfiguration.InputFieldIsDisplayed[i])
                {
                    _inputModels[i].IsDisplayed = false;
                }
            }
        }

        public List<Parameter> CreateParameters()
        {
            List<Parameter> parameters = new List<Parameter>();
            foreach (var inputModel in _inputModels.OfType<InputFieldModel>())
            {
                if (inputModel.InputDataType == InputDataTypeConstants.DATE)
                {
                    parameters.Add(new Parameter(inputModel.Name, typeof(DateTime)));
                }
                else if (inputModel.InputDataType == InputDataTypeConstants.FLOAT)
                {
                    parameters.Add(new Parameter(inputModel.Name, typeof(double)));
                }
                else if(inputModel.InputDataType == InputDataTypeConstants.INT)
                {
                    parameters.Add(new Parameter(inputModel.Name, typeof(double)));
                }
                else if(inputModel.InputDataType == InputDataTypeConstants.NVARCHAR)
                {
                    parameters.Add(new Parameter(inputModel.Name, typeof(string)));
                }
                else if (inputModel.InputDataType == InputDataTypeConstants.TIME)
                {
                    parameters.Add(new Parameter(inputModel.Name, typeof(DateTime)));
                }
            }

            return parameters;
        }

        public List<object> CreateParameterTestValues()
        {
            List<object> testObjects = new List<object>();
            foreach (var inputModel in _inputModels.OfType<InputFieldModel>())
            {
                if (inputModel.InputDataType == InputDataTypeConstants.DATE)
                {
                    testObjects.Add(DateTime.Now);
                }
                else if (inputModel.InputDataType == InputDataTypeConstants.FLOAT)
                {
                    testObjects.Add(0.0);
                }
                else if (inputModel.InputDataType == InputDataTypeConstants.INT)
                {
                    testObjects.Add(0.0);
                }
                else if (inputModel.InputDataType == InputDataTypeConstants.NVARCHAR)
                {
                    testObjects.Add("test");
                }
                else if (inputModel.InputDataType == InputDataTypeConstants.TIME)
                {
                    testObjects.Add(DateTime.Now);
                }
            }

            return testObjects;
        }

        public void CreateSimDataProvider()
        {
            SimDataProvider = new SimDataProvider(this);
        }
        [JsonIgnore]
        public SimDataProvider SimDataProvider { get; set; }

        private DatasetConfiguration _datasetConfiguration = null;
        public DatasetConfiguration DatasetConfiguration
        {
            get
            {
                return _datasetConfiguration;
            }
            set
            {
                this.SetProperty(ref _datasetConfiguration, value);
            }
        }

        public override string Name
        {
            get
            {
                return _datasetConfiguration.Name;
            }
        }

        private List<InputModel> _inputModels = new List<InputModel>();
        public override List<InputModel> InputModels
        {
            get
            {
                return _inputModels;
            }
        }

        private List<OriginModel> _originModels = new List<OriginModel>();
        public override List<OriginModel> OriginModels
        {
            get
            {
                return _originModels;
            }
        }
    }
}
