using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PanoramicDataWin8.controller.data
{private static MainViewController _instance;
    public class Logger
    {
        private static Logger _instance;

        private Logger()
        {
            
        }

        public static void CreateInstance()
        {
            _instance = new Logger();
        }

        public static Logger Instance
        {
            get
            {
                return _instance;
            }
        }

        public async void Log(string msg)
        {
            // saves the string 'content' to a file 'filename' in the app's local storage folder
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(content.ToCharArray());

            // create a file with the given filename in the local folder; replace any existing file with the same name
            StorageFile file = await Windows.Storage.ApplicationData.Current..CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

            // write the char array created from the content string into the file
            using (var stream = await file.OpenStreamForWriteAsync())
            {
                stream.Write(fileBytes, 0, fileBytes.Length);
            }
        }
    }
}
