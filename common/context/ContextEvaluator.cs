using Godot;
using System;

public readonly struct ContextEvaluator
{
	public Node Entity { get; }
	public Node Target { get; }
	public object Context { get; }

	public ContextEvaluator(Node entity, Node target, object context)
	{
		Entity = entity;
		Target = target;
		Context = context;
	}
}
