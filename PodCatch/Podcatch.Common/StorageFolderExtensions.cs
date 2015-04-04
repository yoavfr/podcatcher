using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PodCatch.Common
{
    public static class StorageFolderExtensions
    {
        public async static Task<IStorageFile> TryGetFileAsync(this IStorageFolder storageFolder, string path)
        {
            try
            {
                return await storageFolder.GetFileAsync(path);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async static Task<bool> ItemExists(this IStorageFolder storageFolder, string path)
        {
            return await storageFolder.TryGetFileAsync(path) != null ? true : false;
        }
    }
}
