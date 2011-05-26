using System;
using HP.HPTRIM.SDK;

namespace TrimWeb
{
    abstract class TrimAbstract : IDisposable
    {
        protected readonly Database Db;

        public TrimAbstract(string trimServer, string trimDatasetId)
        {
            Db = new Database
                     {
                         WorkgroupServerName = trimServer
                     };

            if(!String.IsNullOrWhiteSpace(trimDatasetId))
            {
                Db.Id = trimDatasetId;
            }

            Db.Connect();
        }

        public void Dispose()
        {
            Db.Disconnect();
            Db.Dispose();
        }
    }
}
