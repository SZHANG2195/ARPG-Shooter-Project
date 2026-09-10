using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using lethal.gameplay.stats.enums;
using lethal.core.persistence.entities.stats;

namespace lethal.core.persistence;

public static class StatTypeSeeder
{
    private static readonly Dictionary<string, string[]> _tagAssignments = new()
    {
        // Vitals
        ["maxhealth"]       = new[] { "vital", "life" },
        ["healthregen"]     = new[] { "vital", "life" },
        ["maxfuel"]         = new[] { "vital", "fuel" },
        ["fuelregen"]       = new[] { "vital", "fuel" },
        ["maxrage"]         = new[] { "vital", "rage" },
        ["rageregen"]       = new[] { "vital", "rage" },

        // Attributes
        ["strength"]        = new[] { "attribute" },
        ["agility"]         = new[] { "attribute" },
        ["intelligence"]    = new[] { "attribute" },

        // Ammo
        ["maxammo"]         = new[] { "ammo" },
        ["fuelcost"]        = new[] { "ammo", "fuel" },
        ["reloadspeed"]     = new[] { "ammo", "speed" },

        // Damage Types
        ["physicaldamage"]  = new[] { "physical", "alldamage", "attack" },
        ["firedamage"]      = new[] { "fire", "elemental", "alldamage", "elementaldamage", "attack" },
        ["cryodamage"]      = new[] { "cryo", "elemental", "alldamage", "elementaldamage", "attack" },
        ["energydamage"]    = new[] { "energy", "elemental", "alldamage", "elementaldamage", "attack" },
        ["voiddamage"]      = new[] { "void", "elemental", "alldamage", "attack" },

        // Damage Modifiers
        ["attackspeed"]     = new[] { "offense", "speed", "attack" },
        ["critchance"]      = new[] { "offense", "crit", "attack" },
        ["critdamage"]      = new[] { "offense", "crit", "attack" },
        ["lifegainonhit"]   = new[] { "offense", "leech" },
        ["fuelgainonhit"]   = new[] { "offense", "leech" },
        ["rageonhit"]       = new[] { "offense", "rage" },
        ["rageonkill"]      = new[] { "offense", "rage" },

        // Attack Behavior
        ["projectilespeed"] = new[] { "projectile", "speed" },
        ["areaofeffect"]    = new[] { "offense" },
        ["piercecount"]     = new[] { "projectile" },
        ["chaincount"]      = new[] { "projectile" },
        ["ricochetcount"]   = new[] { "projectile" },
        ["projectilecount"] = new[] { "projectile" },

        // Afflictions
        ["ignitechance"]      = new[] { "fire", "affliction", "elementalaffliction", "damageovertime", "elemental" },
        ["ignitemagnitude"]   = new[] { "fire", "affliction", "elementalaffliction", "damageovertime", "allmagnitude", "elemental" },
        ["igniteduration"]    = new[] { "fire", "affliction", "elementalaffliction", "damageovertime", "allduration", "elemental" },
        ["chillchance"]       = new[] { "cryo", "affliction", "elementalaffliction", "elemental" },
        ["chillduration"]     = new[] { "cryo", "affliction", "elementalaffliction", "elemental" },
        ["freezebuildup"]     = new[] { "crowd-control", "cryo", "affliction", "elementalaffliction", "allbuildup", "elemental" },
        ["freezeduration"]    = new[] { "crowd-control", "cryo", "affliction", "elementalaffliction", "allduration", "elemental" },
        ["chargedchance"]     = new[] { "energy", "affliction", "elementalaffliction", "elemental" },
        ["chargedmagnitude"]  = new[] { "energy", "affliction", "elementalaffliction", "allmagnitude", "elemental" },
        ["chargedduration"]   = new[] { "energy", "affliction", "elementalaffliction", "allduration", "elemental" },
        ["voidriftbuildup"]   = new[] { "void", "affliction", "allbuildup" },
        ["voidriftmagnitude"] = new[] { "void", "affliction", "allmagnitude" },
        ["voidriftduration"]  = new[] { "void", "affliction", "allduration" },
        ["bleedchance"]       = new[] { "physical", "affliction", "damageovertime" },
        ["bleedmagnitude"]    = new[] { "physical", "affliction", "damageovertime", "allmagnitude" },
        ["bleedduration"]     = new[] { "physical", "affliction", "damageovertime", "allduration" },
        ["poisonchance"]      = new[] { "void", "affliction", "damageovertime" },
        ["poisonmagnitude"]   = new[] { "void", "affliction", "damageovertime", "allmagnitude" },
        ["poisonduration"]    = new[] { "void", "affliction", "damageovertime", "allduration" },
        ["poisoncap"]         = new[] { "void", "affliction", "damageovertime" },
        ["stunbuildup"]       = new[] { "crowd-control", "affliction", "allbuildup" },
        ["stunduration"]      = new[] { "crowd-control", "affliction", "allduration" },
        ["rootbuildup"]       = new[] { "crowd-control", "affliction", "allbuildup" },
        ["rootduration"]      = new[] { "crowd-control", "affliction", "allduration" },

        // Defenses
        ["armour"]             = new[] { "defense", "armour", "globaldefense" },
        ["evasion"]            = new[] { "defense", "evasion", "globaldefense" },
        ["maxbarrier"]         = new[] { "defense", "vital", "barrier", "globaldefense" },
        ["barrierregen"]       = new[] { "defense", "vital", "barrier" },
        ["blockchance"]        = new[] { "defense" },
        ["glancingchance"]     = new[] { "defense" },
        ["barrierefficiency"]  = new[] { "defense","vital", "barrier" },
        ["fireresistance"]     = new[] { "defense", "fire", "elemental", "allresistance", "elementalresistance" },
        ["cryoresistance"]     = new[] { "defense", "cryo", "elemental", "allresistance", "elementalresistance" },
        ["energyresistance"]   = new[] { "defense", "energy", "elemental", "allresistance", "elementalresistance" },
        ["voidresistance"]     = new[] { "defense", "void", "elemental", "allresistance" },
        ["ragewhenhit"]        = new[] { "defense", "rage" },

        // Damage Taken Modifiers
        ["physicaldamagetaken"]      = new[] { "defense", "physical", "alldamagetaken" },
        ["firedamagetaken"]          = new[] { "defense", "fire", "elemental", "alldamagetaken", "elementaldamagetaken" },
        ["cryodamagetaken"]          = new[] { "defense", "cryo", "elemental", "alldamagetaken", "elementaldamagetaken" },
        ["energydamagetaken"]        = new[] { "defense", "energy", "elemental", "alldamagetaken", "elementaldamagetaken" },
        ["voiddamagetaken"]          = new[] { "defense", "void", "elemental", "alldamagetaken" },
        ["physicaldamagereduction"]  = new[] { "defense", "physical", "alldamagereduction" },
        ["firedamagereduction"]      = new[] { "defense", "fire", "elemental", "alldamagereduction", "elementaldamagereduction" },
        ["cryodamagereduction"]      = new[] { "defense", "cryo", "elemental", "alldamagereduction", "elementaldamagereduction" },
        ["energydamagereduction"]    = new[] { "defense", "energy", "elemental", "alldamagereduction", "elementaldamagereduction" },
        ["voiddamagereduction"]      = new[] { "defense", "void", "elemental", "alldamagereduction" },

        // Crowd Control
        ["stunthreshold"]        = new[] { "crowd-control", "defense" },
        ["stunrecovery"]         = new[] { "crowd-control", "defense" },
        ["afflictionthreshold"]  = new[] { "crowd-control", "defense" },
        ["afflictionduration"]   = new[] { "crowd-control", "defense", "allduration" },
        ["slowresistance"]       = new[] { "crowd-control", "defense", "speed" },

        // Movement
        ["movementspeed"]            = new[] { "movement", "speed" },
        ["movementspeedwhilefiring"] = new[] { "movement", "speed" },

        // Utilities
        ["knockbackforce"]        = new[] { "utility" },
        ["cooldownrecoveryrate"]  = new[] { "utility" },
        ["armingtime"]            = new[] { "utility" },
    };

    private static readonly Dictionary<string, (bool IsPlayerVisible, string DisplayName)> _tagDefinitions = new()
    {
        // Dev-only umbrellas (internal grouping for modifier-matching, never player-craftable)
        ["vital"]                      = (false, "Vital"),
        ["alldamage"]                  = (false, "All Damage"),
        ["elementaldamage"]            = (false, "Elemental Damage"),
        ["allresistance"]              = (false, "All Resistance"),
        ["elementalresistance"]        = (false, "Elemental Resistance"),
        ["alldamagetaken"]             = (false, "All Damage Taken"),
        ["elementaldamagetaken"]       = (false, "Elemental Damage Taken"),
        ["alldamagereduction"]         = (false, "All Damage Reduction"),
        ["elementaldamagereduction"]   = (false, "Elemental Damage Reduction"),
        ["allmagnitude"]               = (false, "All Magnitude"),
        ["allduration"]                = (false, "All Duration"),
        ["allbuildup"]                 = (false, "All Buildup"),
        ["damageovertime"]             = (false, "Damage Over Time"),
        ["defense"]                    = (false, "Defense"),
        ["affliction"]                 = (false, "Affliction"),
        ["elementalaffliction"]        = (false, "Elemental Affliction"),
        ["utility"]                    = (false, "Utility"),

        // Player-visible
        ["life"]           = (true, "Life"),
        ["fuel"]           = (true, "Fuel"),
        ["rage"]           = (true, "Rage"),
        ["attribute"]      = (true, "Attribute"),
        ["ammo"]           = (true, "Ammo"),
        ["speed"]          = (true, "Speed"),
        ["physical"]       = (true, "Physical"),
        ["fire"]           = (true, "Fire"),
        ["cryo"]           = (true, "Cryo"),
        ["energy"]         = (true, "Energy"),
        ["void"]           = (true, "Void"),
        ["elemental"]      = (true, "Elemental"),
        ["attack"]         = (true, "Attack"),
        ["offense"]        = (true, "Offense"),
        ["crit"]           = (true, "Critical"),
        ["leech"]          = (true, "Leech"),
        ["projectile"]     = (true, "Projectile"),
        ["armour"]         = (true, "Armour"),
        ["evasion"]        = (true, "Evasion"),
        ["barrier"]        = (true, "Barrier"),
        ["globaldefense"]  = (true, "Global Defense"),
        ["crowd-control"]  = (true, "Crowd Control"),
        ["movement"]       = (true, "Movement"),
    };

    public static void SeedFromEnum(GameDbContext db)
    {
        var names = Enum.GetNames(typeof(StatType))
            .Where(n => n != nameof(StatType.None))
            .ToList();

        foreach (var name in names)
        {
            string id = name.ToLowerInvariant();
            if (!db.StatDefinitions.Any(s => s.Id == id))
            {
                db.StatDefinitions.Add(new StatDefinitionEntity
                {
                    Id = id,
                    CodeName = name,
                    DisplayName = name,
                    IsRangePaired = false,
                    PairedCounterpartId = null
                });
            }
        }
        db.SaveChanges();

        foreach (var (tagName, meta) in _tagDefinitions)
        {
            if (!db.Set<TagEntity>().Any(t => t.Name == tagName))
            {
                db.Set<TagEntity>().Add(new TagEntity
                {
                    Name = tagName,
                    IsPlayerVisible = meta.IsPlayerVisible,
                    DisplayName = meta.DisplayName
                });
            }
        }
        db.SaveChanges();

        foreach (var (statId, tags) in _tagAssignments)
        {
            foreach (var tag in tags)
            {
                if (!_tagDefinitions.ContainsKey(tag))
                {
                    GD.PrintErr($"[StatTypeSeeder] Warning: Tag '{tag}' used on '{statId}' has no entry in _tagDefinitions — skipping!");
                    continue;
                }

                bool alreadyExists = db.Set<StatTagEntity>().Any(t => t.StatId == statId && t.Tag == tag);
                if (!alreadyExists)
                {
                    db.Set<StatTagEntity>().Add(new StatTagEntity { StatId = statId, Tag = tag });
                }
            }
        }
        db.SaveChanges();

        GD.Print($"[StatTypeSeeder] Seeded {names.Count} stats, {_tagDefinitions.Count} tags, {_tagAssignments.Count} stat-tag assignments.");
    }
}