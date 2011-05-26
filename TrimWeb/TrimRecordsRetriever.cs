using System;
using System.Collections.Generic;
using System.Linq;
using HP.HPTRIM.SDK;
using log4net;

namespace TrimWeb
{
    class TrimRecordsRetriever : TrimAbstract
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TrimRecordsRetriever));

        public TrimRecordsRetriever(string trimServer, string trimDatasetId)
            : base(trimServer, trimDatasetId)
        {
        }

        public DateTime? LastRecordUpdatedDate { get; private set; }

        public IEnumerable<RecordData> RetrieveRecords(DateTime lastUpdated)
        {
            Logger.InfoFormat("Retrieving records from {0:dd/MM/yyyy h:mm tt}", lastUpdated);

            using (var records = new TrimMainObjectSearch(Db, BaseObjectTypes.Record))
            {
                /*
                records.SetSearchString(@"acl:""View Document"",[""INTEL-INVEST""]");   
                records.And();
                
                var trimSearchClause = new TrimSearchClause(BaseObjectTypes.Record, Db, SearchClauseIds.Updated);
                trimSearchClause.SetCriteriaFromDateComparison(ComparisonType.GreaterThanOrEqualTo, new TrimDateTime(lastUpdated));
                records.AddSearchClause(trimSearchClause);
                records.And();

                var extensionSearchClause = new TrimSearchClause(BaseObjectTypes.Record, Db, SearchClauseIds.RecordExtension);
                extensionSearchClause.SetCriteriaFromString("*");
                records.AddSearchClause(extensionSearchClause);

                records.SetFilterString("type:document");
                
                */
                records.SetSearchString(String.Format(@"acl:""View Document"",[""INTEL-INVEST""] and type:document and extension: * and updated >= {0:dd/MM/yyyy h:mm tt} and not (disposition:Inactive,Destroyed)", lastUpdated));
                records.AddSortItemAscending(SearchClauseIds.Updated);

                int recordsReturned = 0;

                foreach (Record resultRecord in records)
                {
                    Logger.DebugFormat("{0:dd MMM yyyy HH:mm:ss} - {1}", resultRecord.LastUpdatedOn.ToDateTime(), resultRecord.NameString);
                    var data = new RecordData {RecordNumber = resultRecord.Uri};
                    data.Metadata.Add("name", resultRecord.NameString);
                    if (!String.IsNullOrEmpty(resultRecord.Notes))
                    {
                        data.Metadata.Add("notes", resultRecord.Notes);
                    }
                    using (ExtractDocument extractDocument = resultRecord.GetExtractDocument())
                    {
                        data.Metadata.Add("filename", extractDocument.FileName);
                    }
                    data.Acls = RetrieveSecurity(resultRecord).ToList();
                    
                    yield return data;

                    if (++recordsReturned == 100)
                    {
                        Logger.Info("Returning only 100 records");
                        LastRecordUpdatedDate = resultRecord.LastUpdatedOn.ToDateTime();
                        yield break;
                    }
                }
            }
        }

        private static IEnumerable<String> RetrieveSecurity(Record record)
        {
            AccessControlSettings accessControlSettings =
                record.AccessControlList.get_CurrentSetting((int)RecordAccess.ViewDocument);

            if (accessControlSettings == AccessControlSettings.Inherited && record.IsEnclosed)
            {
                foreach (string s in RetrieveSecurity(record.Container))
                {
                    yield return s;
                }
            }

            if (accessControlSettings == AccessControlSettings.Private)
            {
                Location[] accessLocations = record.AccessControlList.get_AccessLocations((int)RecordAccess.ViewDocument);

                foreach (Location accessLocation in accessLocations)
                {
                    switch (accessLocation.TypeOfLocation)
                    {
                        case LocationType.Person:
                            yield return accessLocation.LogsInAs;
                            break;
                        
                        case LocationType.Group:
                            yield return accessLocation.NameString;
                            break;
                    }
                }
            }
        }
    }
}