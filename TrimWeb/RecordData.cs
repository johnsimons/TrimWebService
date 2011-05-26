using System.Collections.Generic;

namespace TrimWeb
{
	class RecordData
	{
	    public RecordData()
	    {
	        Acls = new List<string>();
            Metadata = new Dictionary<string, string>();
	    }

		public long RecordNumber { get; set; }
		public List<string> Acls { get; set; }
		public Dictionary<string, string> Metadata { get; set; }
	}
}