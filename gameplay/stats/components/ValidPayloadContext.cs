using lethal.core.domain.payloads;
using lethal.gameplay.payloads.instances;

namespace lethal.gameplay.stats.components;
public sealed partial class StatsComponent
{
	private readonly struct ValidPayloadContext
	{
    	public StatPayloadInstance Instance { get; init; }
    	public IPayload PayloadData { get; init; }
    	public double Value { get; init; }
    	public bool IsSecondary { get; init; }
	}
}

