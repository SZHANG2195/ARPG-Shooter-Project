using System;
using System.Collections.Generic;

namespace lethal.gameplay.stats.components;
public sealed partial class StatsComponent
{
	private readonly struct PooledPayloadList : IDisposable
	{
    	private readonly StatsComponent _component;
    	public readonly List<ValidPayloadContext> List;

    	public PooledPayloadList(StatsComponent component)
    	{
        	_component = component;
        	List = _component._validPayloadPool.Count > 0 
            ? _component._validPayloadPool.Pop() 
            : new List<ValidPayloadContext>(128);
    	}

    	public void Dispose()
    	{
        	List.Clear();
        	_component._validPayloadPool.Push(List);
    	}
	}
}