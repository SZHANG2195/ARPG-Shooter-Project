using System.Collections.Generic;

namespace lethal.gameplay.stats.components;
public sealed partial class StatsComponent
{
	private readonly ref struct PooledPayloadList
	{
		private static readonly Stack<List<ValidPayloadContext>> _validPool = new();
    	public readonly List<ValidPayloadContext> List;

		private PooledPayloadList(List<ValidPayloadContext> list) => List = list;

    	public static PooledPayloadList Rent() => new(_validPool.Count > 0 ? _validPool.Pop() : new List<ValidPayloadContext>(64));

    	public void Dispose()
    	{
			if (List is null) return;
        	List.Clear();
        	_validPool.Push(List);
    	}
	}
}