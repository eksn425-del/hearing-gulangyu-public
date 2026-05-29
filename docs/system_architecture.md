# System Architecture

## Public-Safe Overview

```text
Site / route data
      |
      v
Graph routing engine
      |
      +-- route weighting by distance, slope, friction, user profile
      +-- direction and turn-instruction generation
      |
      v
Navigation app prototype
      |
      v
Unity digital twin simulation
      |
      +-- digital cane ray scanning
      +-- hazard zone detection
      +-- route following
      +-- exposure counting
      |
      v
Evaluation charts and portfolio narrative
```

## Backend Sample Layer

The backend code samples show:

- graph construction
- route-weight calculation
- bearing and turn-instruction logic
- simulated route evaluation

Raw database and CSV files are excluded from this public repository.

## Unity Sample Layer

The Unity samples show:

- digital cane hazard sensing
- hazard zone representation
- route-following behavior
- dynamic obstacle exposure

Complete Unity assets and project folders are excluded.

## Showcase Layer

The repository includes selected public-safe charts and map assets to communicate the system:

- route comparison
- intent distribution
- accuracy and baseline comparison
- confusion matrix
- A/B test summary figures

