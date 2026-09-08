namespace lethal.gameplay.stats.modifiers;
public class StaticStatModifier : StatModifier
{
	public float Value { get; set; }

    public override float GetValue() => Value;

}
