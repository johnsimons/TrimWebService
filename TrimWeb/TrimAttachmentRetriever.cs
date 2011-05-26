using System;
using System.Runtime.ExceptionServices;
using HP.HPTRIM.SDK;
using System.IO;
using log4net;

namespace TrimWeb
{
    class TrimAttachmentRetriever : TrimAbstract
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TrimAttachmentRetriever));

        public TrimAttachmentRetriever(string trimServer, string trimDatasetId)
            : base(trimServer, trimDatasetId)
        {
        }

        public string FileName { get; private set; }

        public string MimeType { get; private set; }

        [HandleProcessCorruptedStateExceptions]
        public string GetTrimAttachmentPath(long recordNumber)
        {
            string filePath = null;

            using (var records = new TrimMainObjectSearch(Db, BaseObjectTypes.Record))
            {
                records.SelectByUris(new[] { recordNumber });

                foreach (Record resultRecord in records)
                {
                    using (var extractDocument = resultRecord.GetExtractDocument())
                    {
                        FileName = extractDocument.FileName;
                        MimeType = resultRecord.MimeType;

                        Logger.InfoFormat("Retrieving trim attachment for '{0}'", FileName);
                        extractDocument.FileName = Path.GetRandomFileName();
                        try
                        {
                            extractDocument.DoExtract(Path.GetTempPath(), true, false, String.Empty);   
                        }
                        catch (AccessViolationException ex)
                        {
                            Logger.Error(String.Format("Failed to extract attachment from trim '{0}'", FileName), ex);
                            continue;
                        }
                        
                        filePath = Path.Combine(Path.GetTempPath(), extractDocument.FileName);
                    }
                    break;
                }
            }

            return filePath;
        }
    }
}
