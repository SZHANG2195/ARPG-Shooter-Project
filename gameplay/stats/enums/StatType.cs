namespace lethal.gameplay.stats.enums;
public enum StatType {

    //Default  = 0
    None = 0,
    
    //Vitals = 100
    MaxHealth = 100,
    HealthRegen = 101,
    MaxFuel = 102,
    FuelRegen = 103,
    MaxRage = 104,
    RageRegen = 105,

    //Attributes = 200
    Strength = 200,
    Agility = 201,
    Intelligence = 202,

    //Ammo = 300
    MaxAmmo = 300,
    FuelCost = 301,
    ReloadSpeed = 302,

    //Damage Types = 400
    PhysicalDamage = 400,
    FireDamage = 401,
    CryoDamage = 402,
    EnergyDamage = 403,
    VoidDamage = 404,

    //Damage Modifiers = 500
    AttackSpeed = 500,
    CritChance = 501,
    CritDamage = 502,
    LifeGainOnHit = 503,
    FuelGainOnHit = 504,
    RageOnHit = 505,
    RageOnKill = 506,

    //Attack Behavior = 600
    ProjectileSpeed = 600,
    AreaOfEffect = 601,
    PierceCount = 602,
    ChainCount = 603,
    RicochetCount = 604,
    ProjectileCount = 605,

    //Ailments = 700
    IgniteChance = 700,
    IgniteMagnitude = 701,
    IgniteDuration = 702,
    ChillChance = 703,
    ChillDuration = 704,
    FreezeBuildup = 705,
    FreezeDuration = 706,
    ChargedChance = 707,
    ChargedMagnitude = 708,
    ChargedDuration = 709,
    VoidRiftBuildup = 710,
    VoidRiftMagnitude = 711,
    VoidRiftDuration = 712,
    BleedChance = 713,
    BleedMagnitude = 714,
    BleedDuration = 715,
    PoisonChance = 716,
    PoisonMagnitude = 717,
    PoisonDuration = 718,
    PoisonCap = 719,
    StunBuildup = 720,
    StunDuration = 721,
    RootBuildup = 722,
    RootDuration = 723,

    //Defenses = 800
    Armour = 800,
    Evasion = 801,
    MaxBarrier = 802,
    BarrierRegen = 803,
    BlockChance = 804,
    GlancingChance = 805,
    BarrierEfficiency = 806,
    FireResistance = 807,
    CryoResistance = 808,
    EnergyResistance = 809,
    VoidResistance = 810,
    RageWhenHit = 811,

    //Damage Taken Modifiers = 900
    PhysicalDamageTaken = 900, 
    FireDamageTaken = 901, 
    CryoDamageTaken = 902, 
    EnergyDamageTaken = 903, 
    VoidDamageTaken = 904,
    PhysicalDamageReduction = 905, 
    FireDamageReduction = 906, 
    CryoDamageReduction = 907, 
    EnergyDamageReduction = 908, 
    VoidDamageReduction = 909,

    //Crowd Control = 1000
    StunThreshold = 1000,
    StunRecovery = 1001,
    AfflictionThreshold = 1002,
    AfflictionDuration = 1003,
    SlowResistance = 1004,

    //Movement = 1100
    MovementSpeed = 1100,
    MovementSpeedWhileFiring = 1101,

    //Utilities = 1200
    KnockbackForce = 1200,
    CoolDownRecoveryRate = 1201,
    ArmingTime = 1202

}