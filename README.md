# Hearing Gulangyu

Accessible Heritage Navigation & Digital Twin System

Hearing Gulangyu is a completed cross-disciplinary project that combines an accessibility-oriented navigation app, Unity-based digital twin simulation, route-risk modeling, sensory heritage mapping, and evaluation charts for Gulangyu Island.

This repository is a public-safe flagship showcase. It is not the full private project archive and does not include raw data, deployment files, database files, environment variables, internal academic documents, or complete Unity/App source trees.

## Why This Project Matters

Historic heritage sites are often difficult to navigate for elderly visitors, visually impaired users, wheelchair users, and first-time tourists. Gulangyu Island adds another layer of complexity: dense alleys, slopes, changing pavements, heritage buildings, crowd flows, and sensory memory.

This project explores how digital navigation can move beyond shortest-path routing and become a more inclusive spatial experience.

## What I Built

- A navigation logic prototype for accessibility-aware route selection.
- A graph-based routing engine using slope, friction, distance, and user profiles.
- A Unity digital twin simulation with a digital cane, hazard zones, route following, and exposure counting.
- Sensory and heritage-mapping assets for site storytelling.
- Evaluation figures comparing route behavior, hazard exposure, completion time, and algorithm performance.
- A portfolio-ready system narrative connecting app, simulation, and research workflow.

## Repository Map

- `docs/` - project narrative, system architecture, privacy notes, and release notes
- `assets/figures/` - selected public-safe charts and visual materials
- `code_samples/backend/` - selected Python routing/simulation samples
- `code_samples/unity/` - selected Unity C# simulation samples

## Selected Figures

### Accessibility Routing Comparison

![Routing comparison](assets/figures/routing_comparison.png)

### Intent Distribution

![Intent distribution](assets/figures/intent_distribution.png)

### Evaluation Summary

![A/B summary](assets/figures/paper_ab_summary.png)

### Gulangyu Map

![Gulangyu map](assets/figures/gulangyu_map.jpg)

## Technical Highlights

- Multi-profile route weighting for normal, elderly, disabled, and blind-user scenarios.
- Route weights combining distance, slope, surface friction, and accessibility constraints.
- Direction and turn-instruction logic for navigation guidance.
- Unity ray-based digital cane prototype for hazard detection.
- Hazard exposure metrics for evaluating route safety.
- Public-safe chart set for communicating results without exposing raw data.

## Code Samples

Backend:

- [`graph_engine.py`](code_samples/backend/graph_engine.py)
- [`simulation.py`](code_samples/backend/simulation.py)

Unity:

- [`DigitalCane.cs`](code_samples/unity/DigitalCane.cs)
- [`RouteFollower.cs`](code_samples/unity/RouteFollower.cs)
- [`HazardZone.cs`](code_samples/unity/HazardZone.cs)
- [`DynamicHazardObstacle.cs`](code_samples/unity/DynamicHazardObstacle.cs)

These are selected samples for review. They are not a full runnable production package.

## Privacy And Safety

This public repository intentionally excludes:

- `.env` files and deployment configuration
- SSH keys, tokens, passwords, API keys, and database credentials
- raw databases, user samples, CSV data, and private logs
- ngrok/public demo URLs
- teacher/advisor/team/internal academic materials
- full paper drafts and submission materials
- Unity `Library`, `Temp`, `Obj`, and `Logs`
- complete raw APP and Unity project archives

See [`docs/public_release_notes.md`](docs/public_release_notes.md) for details.

## Status

Completed project, public-safe showcase release.

The full private archive is stored offline and is not published.

