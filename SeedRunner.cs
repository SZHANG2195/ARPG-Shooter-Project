 using Godot;
   using lethal.core.persistence;

   public partial class SeedRunner : Node
   {
       public override void _Ready()
       {
           using var db = new GameDbContext();
           StatTypeSeeder.SeedFromEnum(db);
       }
   }
