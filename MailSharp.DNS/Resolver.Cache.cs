using System.Collections.Concurrent;

namespace MailSharp.DNS;

public partial class Resolver
{
	private readonly ConcurrentDictionary<string, List<RR>> recordCache = [];
	private Response? SearchInRecordCache(QuestionRecord question)
	{
		if (!useCache)
			return null;

		List<RR> list = [];
		if (recordCache.TryGetValue(question.Name, out List<RR>? value))
		{
			list = value.Where(x => x.TTL > 0 && x.Class == (DnsClass)question.Class &&
			(question.Type == DnsQType.ANY || x.Type == (DnsType)question.Type))
				.ToList();
		}
		if (list.Count == 0)
			return null;

		Response response = new()
		{
			Questions = [question],
			Authorities = list
		};

		response.Header.QDCount = (ushort)response.Questions.Count;
		response.Header.NSCount = (ushort)list.Count;
		return response;
	}

	private void AddToCache(Response response)
	{
		if (!useCache || response.Questions.Count == 0 || response.Header.RCODE != RCode.NoError)
			return;

		foreach (var rr in response.RecordsRR)
		{
			rr.TimeStamp = DateTime.Now;
			recordCache.AddOrUpdate(rr.Name,
				[rr],
				(_, list) =>
				{
					list.Add(rr);
					return list;
				});
		}
	}

	internal class QuestionComparer : IEqualityComparer<QuestionRecord>
	{
		public int GetHashCode(QuestionRecord obj)
		{
			int hash = 17;
			hash = hash * 31 + obj.Class.GetHashCode();
			hash = hash * 31 + obj.Type.GetHashCode();
			hash = hash * 31 + (obj.Name != null ? StringComparer.Ordinal.GetHashCode(obj.Name) : 0);
			return hash;
		}
		public bool Equals(QuestionRecord? x, QuestionRecord? y)
		{
			if(x == null || y == null)
				return false;
			if(x.Class != y.Class)
				return false;
			if (x.Type != y.Type)
				return false;
			return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
		}
	}
}