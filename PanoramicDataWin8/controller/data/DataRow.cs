using System.Collections.Generic;
using System.Linq;
using PanoramicDataWin8.model.data;
using PanoramicDataWin8.model.data.result;
using PanoramicDataWin8.model.data.sim;

namespace PanoramicDataWin8.controller.data
{
    public class DataRow : ResultItemModel
    {
        private Dictionary<InputFieldModel, object> _entries = null;
        public Dictionary<InputFieldModel, object> Entries
        {
            get
            {
                return _entries;
            }
            set
            {
                _entries = value;
            }
        }

        private Dictionary<InputOperationModel, double?> _visualizationValues = new Dictionary<InputOperationModel, double?>();
        public Dictionary<InputOperationModel, double?> VisualizationValues
        {
            get
            {
                return _visualizationValues;
            }
            set
            {
                _visualizationValues = value;
            }
        }

        public List<object> CreateRowValues(QueryModel queryModel)
        {
            List<object> values = new List<object>();

            foreach (var inputModel in ((SimSchemaModel)queryModel.SchemaModel).RootOriginModel.InputModels.OfType<InputFieldModel>())
            {
                values.Add(this.Entries[inputModel]);
            }
            return values;
        }
    }
}