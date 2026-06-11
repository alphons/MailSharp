namespace MailSharp.DNS;

public abstract class DnsResourceRecord<TType, TClass>
	where TType : struct, Enum
	where TClass : struct, Enum
{
	public string Name { get; init; } = string.Empty;
	public TType Type { get; init; }
	public TClass Class { get; init; }

	protected DnsResourceRecord(string name, TType type, TClass @class)
	{
		Name = name.EndsWith('.') ? name : name + ".";
		Type = type;
		Class = @class;
	}

	protected DnsResourceRecord()
	{
	}

	public override string ToString() => $"{Name,-32}\t{Class}\t{Type}";

	public override int GetHashCode()
	{
		int hash = 17;
		hash = hash * 31 + Class.GetHashCode();
		hash = hash * 31 + Type.GetHashCode();
		hash = hash * 31 + (Name != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Name) : 0);
		return hash;
	}
}