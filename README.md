# Data-Driven ARPG Calculation Engine

A modular C# stat calculation engine designed for top-down action RPGs, featuring automated CSV data ingestion and a multi-pass evaluation pipeline built to handle complex modifier dependency trees.

## Core Features & Architecture

- **Automated Ingestion Pipeline:** Parses CSV game configurations into local relational structures for runtime entity generation, utilizing lookahead regex splitting to preserve quoted multi-select dropdown fields.
- **Two-Pass Bitmask Evaluation:** Isolates primary attributes (Strength, Agility, Intelligence) into a pre-computation pass using bitmask flags and dependency isolation, preventing recursive loops during modifier resolution.
- **Complex Stat Scaling:** Supports flat additions, additive percent chains (`Increased`), and multiplicative scaling chains (`More`) modeled after Path of Exile 2 mechanics.
- **Secondary Threshold Resolution:** Computes post-multiplier derived stats and dynamic gameplay thresholds (e.g., status ailment and stun thresholds based on max health pool).

## Tech Stack
- **Language:** C#
- **Engine / Environment:** Godot
- **Data Storage & Ingestion:** SQLite, CSV
- 
## Currently WIP
