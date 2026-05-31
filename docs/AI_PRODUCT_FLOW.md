# AI Product Flow

## Product Goal

Hearing Gulangyu aims to provide a more accessible visitor experience in a complex heritage site by combining AI interaction, site knowledge, route-risk algorithms, and digital-twin validation.

## Core Loop

```text
Visitor input
-> AI interpretation
-> map and heritage context grounding
-> route-risk algorithm / guide logic
-> accessible answer or route suggestion
-> Unity validation and metric review
-> prompt, weight, or route iteration
```

## Input Layer

The system is designed around several visitor inputs:

- voice question
- text question
- photo-based question
- current location
- destination or point of interest
- user type: normal, elderly, wheelchair user, blind or low-vision visitor
- navigation preference: shortest, safer, more accessible, heritage-oriented

## AI Interpretation Layer

The AI API is used as an interaction layer, but it is not treated as a generic chatbot. It supports:

- understanding visitor intent
- correcting or clarifying noisy voice input
- interpreting photo-based questions
- producing natural-language guide responses
- explaining route and heritage context in a friendlier way

## Spatial Grounding Layer

The system constrains AI responses with:

- Gulangyu map nodes
- heritage building points
- sensory notes such as sound, smell, pavement, and atmosphere
- slope, friction, and route-risk weights
- user profile and accessibility constraints

This makes the AI answer closer to the actual site rather than a general tourist answer.

## Algorithmic Decision Layer

The route engine calculates route weights using:

- distance
- slope
- surface friction
- user profile sensitivity
- route strategy such as safer or shortest

The product logic is that AI can explain or present the result, while the algorithm provides structured route constraints.

## Output Layer

Possible outputs include:

- accessible route recommendation
- turn-by-turn instruction
- route safety warning
- heritage explanation
- sensory guide response
- clarification question when input is ambiguous
- photo-based visitor assistance

## Validation Layer

Unity digital twin simulation validates the experience through:

- route following
- digital cane ray scanning
- hazard zone detection
- dynamic obstacle exposure
- route completion time
- A/B comparison figures

## Human Feedback Layer

The designer can adjust:

- route weights
- user profile sensitivity
- map context prompts
- heritage point descriptions
- voice correction fallback logic
- Unity hazard-zone setup

## Product Claim

This project is an AI-assisted spatial experience product. It combines AI API interaction with route algorithms and site context to improve accessibility and guide quality for a real heritage environment.
