using System.Collections.Generic;
using System.Threading.Tasks;
using PanoramicDataWin8.model.data;

namespace PanoramicDataWin8.controller.data
{
    public abstract class DataProvider
    {
        public bool IsInitialized { get; set; }
        public abstract void StartSampling();
        public abstract DataPage GetSampleDataRows(int from, int sampleSize);
        public abstract int GetNrTotalSamples();
    }

    public class DataPage
    {
        public List<DataRow> DataRows {get; set; }
    }
}
