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

        private Logger()
        {
            
        }

        public static async Task<Logger> CreateInstance(MainModel mainModel)
        {
            _instance?._stream.Flush();

            _instance = new Logger();
            var fileName = DateTime.Now.Ticks.ToString() + "#" + mainModel.Participant + "_" + mainModel.Mode.ToString() + "_" + mainModel.Dataset.ToString();
            StorageFile file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            _instance._stream = await file.OpenStreamForWriteAsync();
            return _instance;
        }

        public static Logger Instance
        {
            get
            {
                return _instance;
            }
        }

        private async void log(string msg)
        {
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes((msg + "\n").ToCharArray());
            _stream.Write(fileBytes, 0, fileBytes.Length);
            _stream.Flush();
        }

        private string queryModelToString(QueryModel queryModel)
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
            else
            {
                return "n/a";
            }
        }

        public void LogQueryResult(string evt, QueryModel qm, ResultModel resultModel)
        {
            JObject data = new JObject(
                new JProperty("timestamp", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff")),
                new JProperty("evt", evt),
                new JProperty("visualization", queryModelToString(qm)),
                new JProperty("payload", JsonConvert.DeserializeObject(JsonConvert.SerializeObject(resultModel, Formatting.Indented, new KeysJsonConverter(typeof(InputOperationModel))))));

            log(data.ToString());
        }

        public void Log(string evt)
        {
            JObject data = new JObject(
                new JProperty("timestamp", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff")),
                new JProperty("evt", evt));

            log(data.ToString());
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
                    new JValue((((InputOperationModel)value).InputModel as SimInputFieldModel).Name).WriteTo(writer);
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
