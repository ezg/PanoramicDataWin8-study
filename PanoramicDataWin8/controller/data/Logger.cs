using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PanoramicDataWin8.controller.view;
using PanoramicDataWin8.model.data;
using PanoramicDataWin8.model.data.result;
using PanoramicDataWin8.model.data.sim;
using PanoramicDataWin8.model.view;

namespace PanoramicDataWin8.controller.data
{
    public class Logger
    {
        private static Logger _instance;
        private Stream _stream = null;
        private Stream _mouseStream = null;


        private Logger()
        {
            
        }

        public static async Task<Logger> CreateInstance(MainModel mainModel)
        {
            _instance?._stream.Flush();
            _instance?._mouseStream.Flush();

            _instance = new Logger();
            var fileName = "LOG_" + DateTime.Now.Ticks.ToString() + "#" + mainModel.Participant + "_" + mainModel.Mode.ToString() + "_" + mainModel.Dataset.ToString() + "_" + mainModel.DelayInMs.ToString() + "_" + mainModel.Seed;
            StorageFile file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            _instance._stream = await file.OpenStreamForWriteAsync();

            fileName = "MOUSE_" + DateTime.Now.Ticks.ToString() + "#" + mainModel.Participant + "_" + mainModel.Mode.ToString() + "_" + mainModel.Dataset.ToString() + "_" + mainModel.DelayInMs.ToString() + "_" + mainModel.Seed;
            file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            _instance._mouseStream = await file.OpenStreamForWriteAsync();

            return _instance;
        }

        public static Logger Instance
        {
            get
            {
                return _instance;
            }
        }

        private async void log(string msg, Stream stream)
        {
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes((msg + "\n").ToCharArray());
            stream?.WriteAsync(fileBytes, 0, fileBytes.Length);
        }

        private string queryModelToString(QueryModel queryModel)
        {
            if (queryModel != null)
            {
                if (MainViewController.Instance.VisualizationViewModel1.QueryModel.Id == queryModel.Id)
                {
                    return "v1";
                }
                if (MainViewController.Instance.VisualizationViewModel2.QueryModel.Id == queryModel.Id)
                {
                    return "v2";
                }
                if (MainViewController.Instance.VisualizationViewModel3.QueryModel.Id == queryModel.Id)
                {
                    return "v3";
                }
                if (MainViewController.Instance.VisualizationViewModel4.QueryModel.Id == queryModel.Id)
                {
                    return "v4";
                }
                return "n/a";
            }
            return "";
        }

        public void LogQueryResult(string evt, QueryModel qm, ResultModel resultModel, string brushQuery, string filterQuery)
        {
            JObject data = new JObject(
                new JProperty("timestamp", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff")),
                new JProperty("evt", evt),
                new JProperty("visualization", queryModelToString(qm)),
                new JProperty("filterQuery", filterQuery),
                new JProperty("brushQuery", brushQuery),
                new JProperty("payload", JsonConvert.DeserializeObject(JsonConvert.SerializeObject(resultModel, Formatting.Indented, new KeysJsonConverter(typeof(InputOperationModel))))));

            log(data.ToString(), _stream);
        }

        public void Log(string evt)
        {
            JObject data = new JObject(
                new JProperty("timestamp", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff")),
                new JProperty("evt", evt));

            log(data.ToString(), _stream);
        }

        public void Log(string evt, params JProperty[] properties)
        {
            JObject data = new JObject(
                new JProperty("timestamp", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff")),
                new JProperty("evt", evt));

            foreach (var jProperty in properties)
            {
                data.Add(jProperty);
            }

            log(data.ToString(), _stream);
        }

        public void Log(string evt, QueryModel qm, params JProperty[] properties)
        {
            var p = properties.ToList();
            p.Insert(0, new JProperty("visualization", queryModelToString(qm)));
            Log(evt, p.ToArray());
        }

        public void LogMouse(string evt, double x, double y, bool? rightButton = null)
        {
            JObject data = new JObject(
                new JProperty("timestamp", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff")),
                new JProperty("evt", evt),
                new JProperty("x", x),
                new JProperty("y", y));

            if (rightButton.HasValue)
            {
                data.Add(new JProperty("rightButton", rightButton.Value));
            }

            log(data.ToString(), _mouseStream);
        }

        public void Flush()
        {
            _mouseStream?.FlushAsync();
            _stream?.FlushAsync();
        }
    }

    public class KeysJsonConverter : JsonConverter
    {
        private readonly Type[] _types;

        public KeysJsonConverter(params Type[] types)
        {
            _types = types;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken t = JToken.FromObject(value);
            if (t.Type != JTokenType.Object)
            {
                t.WriteTo(writer);
            }
            else
            {
                if (value is InputOperationModel)
                {
                    var obj = new JObject(
                        new JProperty("attribute", ((SimInputFieldModel) ((InputOperationModel) value).InputModel).Name),
                        new JProperty("aggregation", ((InputOperationModel) value).AggregateFunction.ToString()));
                    obj.WriteTo(writer);
                }
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return _types.Any(t => t == objectType);
        }
    }
}
