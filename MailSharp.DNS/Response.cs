using MailSharp.DNS.Records;

namespace MailSharp.DNS;

public class Response
{
	private readonly byte[] data = [];

	public List<QuestionRecord> Questions { get; set; } = [];
	public List<RR> Answers { get; set; } = [];
	public List<RR> Authorities { get; set; } = [];
	public List<RR> Additionals { get; set; } = [];

	/// <summary>
	/// Error message, empty when no error
	/// </summary>
	public string Error => Header.RCODE == RCode.NoError ? string.Empty : Header.RCODE.ToString();

	public string ErrorMessage { get; set; } = string.Empty;
	/// <summary>
	/// TimeStamp when cached
	/// </summary>
	public DateTime TimeStamp;

	public Response()
	{
		this.data = new byte[12];

		TimeStamp = DateTime.Now;
	}

	public int MessageSize => this.data.Length;

	public DnsHeader Header => new(this.data);

	public Response(byte[] data)
	{
		this.data = data;

		RecordReader rr = new(this.data, 12);
		for (int intI = 0; intI < this.Header.QDCount; intI++)
		{
			this.Questions.Add(new QuestionRecord(ref rr));
		}
		for (int intI = 0; intI < this.Header.ANCount; intI++)
		{
			this.Answers.Add(new RR(rr));
		}
		for (int intI = 0; intI < this.Header.NSCount; intI++)
		{
			this.Authorities.Add(new RR(rr));
		}
		for (int intI = 0; intI < this.Header.ARCount; intI++)
		{
			this.Additionals.Add(new RR(rr));
		}

		TimeStamp = DateTime.Now;
	}

	/// <summary>
	/// List of RecordMX in Response.Answers
	/// </summary>
	public RecordMX[] RecordsMX => [.. Answers.Select(a => a.Record).OfType<RecordMX>()];

	/// <summary>
	/// List of RecordTXT in Response.Answers
	/// </summary>
	public RecordTXT[] RecordsTXT => [.. Answers.Select(a => a.Record).OfType<RecordTXT>()];

	/// <summary>
	/// List of RecordA in Response.Answers
	/// </summary>
	public RecordA[] RecordsA => [.. Answers.Select(a => a.Record).OfType<RecordA>()];

	/// <summary>
	/// List of RecordPTR in Response.Answers
	/// </summary>
	public RecordPTR[] RecordsPTR => [.. Answers.Select(a => a.Record).OfType<RecordPTR>()];

	/// <summary>
	/// List of RecordCNAME in Response.Answers
	/// </summary>
	public RecordCNAME[] RecordsCNAME => [.. Answers.Select(a => a.Record).OfType<RecordCNAME>()];

	/// <summary>
	/// List of RecordAAAA in Response.Answers
	/// </summary>
	public RecordAAAA[] RecordsAAAA => [.. Answers.Select(a => a.Record).OfType<RecordAAAA>()];

	/// <summary>
	/// List of RecordNS in Response.Answers
	/// </summary>
	public RecordNS[] RecordsNS => [.. Answers.Select(a => a.Record).OfType<RecordNS>()];

	/// <summary>
	/// List of RecordSOA in Response.Answers
	/// </summary>
	public RecordSOA[] RecordsSOA => [.. Answers.Select(a => a.Record).OfType<RecordSOA>()];

	public RR[] RecordsRR
	{
		get
		{
			List<RR> list = [.. this.Answers, .. this.Authorities, .. this.Additionals];
			return [.. list];
		}
	}
}
