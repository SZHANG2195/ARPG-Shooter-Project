# Data-Driven ARPG Prototype - Core Stats

The core stats and modifier pipeline for a custom-built Action RPG prototype, featuring a reactive dependency graph and PoE-inspired scaling mechanics.

## Core Features & Architecture

- **Dependency Graph & Granular Invalidation:** Utilizes a topological sorting StatGraph and surgical dirty-flagging (_statGraph.CollectDependents). Changes to stats or equipment invalidate only downstream dependencies rather than triggering expensive global recalculations.
- **Two-Tier Performance Model:** Incurs a brief 1–7.6 ms resolution cost only when a stat's dependencies change, while serving continuous combat queries via a sub-microsecond (~0.087 µs) cached read path.
- **Advanced Modifier Pipeline:** Supports flat additions, default overrides, flat overrides, conversions, raw/mirrored/scoped percent-additive (Increased) chains, and multiplicative (More) scaling chains.
- **Dynamic Derivation & Thresholds:** Automatically computes derived stats and gameplay thresholds (e.g., dynamic stun thresholds derived from health pools) across isolated stat state lanes (Mainhand, Offhand, Default).
- **Zero-Allocation Data Paths:** Leverages readonly record struct snapshots (StatResult), pooled payload lists (PooledPayloadList), and ReadOnlySpan<T> processing to eliminate garbage collection pressure during active gameplay.

## Tech Stack
- **Language:** C# (.NET)
- **Engine / Environment:** Godot
- **Data & Sync Layer:** PostgreSQL (Supabase), SQLite local cache, and automated JSON payload registries
- 
## Currently WIP
