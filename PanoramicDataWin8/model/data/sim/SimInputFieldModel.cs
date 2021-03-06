﻿using Newtonsoft.Json;

namespace PanoramicDataWin8.model.data.sim
{
    [JsonObject(MemberSerialization.OptOut)]
    public class SimInputFieldModel : InputFieldModel
    {
        public SimInputFieldModel(string name, string inputDataType, string inputVisualizationType)
        {
            _name = name;
            _inputDataType = inputDataType;
            _inputVisualizationType = inputVisualizationType;
        }

        private string _name = "";
        public override string Name
        {
            get
            {
                return _name;
            }
            set { this.SetProperty(ref _name, value); }
        }

        private string _inputVisualizationType = "";
        public override string InputVisualizationType
        {
            get
            {
                return _inputVisualizationType;
            }
        }

        private string _inputDataType = "";
        public override string InputDataType
        {
            get
            {
                return _inputDataType;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
