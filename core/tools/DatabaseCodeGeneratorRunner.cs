using Godot;

namespace lethal.core.tools;
public static partial class DatabaseCodeGeneratorRunner
{
    public static void RunGenerationPipeline()
    {
		if (!OS.HasFeature("editor"))
        {
            return; 
        }

        GD.Print("[CodeGenerator] Starting code generation pipeline...");

        StatsClassGenerator.Generate();
        TagsClassGenerator.Generate();

        GD.Print("[CodeGenerator] Pipeline complete.");
    }
}