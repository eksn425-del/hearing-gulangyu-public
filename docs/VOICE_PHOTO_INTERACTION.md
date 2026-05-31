# Voice and Photo Interaction

## Voice Interaction

Voice input supports visitors who may not want to type while walking. It is also important for accessibility scenarios, especially for blind or low-vision users.

## Voice Correction Logic

The product should treat speech recognition as uncertain. A strong interaction flow includes:

```text
speech input
-> transcription
-> AI correction / intent normalization
-> map entity matching
-> confirmation if confidence is low
-> guide answer or route action
```

## Voice Use Cases

- "Take me to the nearest accessible route."
- "What building is near me?"
- "Is this road steep?"
- "I want a quieter route."
- "Repeat the next turn."

## Photo Interaction

Photo input supports on-site problem solving:

```text
visitor photo
-> image/question interpretation
-> map or route context matching
-> AI guide response
-> safety or accessibility suggestion
```

## Photo Use Cases

- recognizing a building or facade
- asking what a sign means
- identifying whether a path looks safe
- asking about an obstacle or pavement condition
- requesting nearby heritage explanation

## Product Boundary

The public repository does not expose private API keys or raw app implementation. The product documentation explains the interaction design and system role of AI, while selected code samples show the routing and simulation logic.

## UX Principle

AI should reduce interaction friction. If the visitor's speech or photo question is unclear, the system should help recover gracefully instead of failing silently.
