using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PanoramicDataWin8.model.data.common
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AxisType { Ordinal, Quantitative, Nominal, Time, Date }
}
