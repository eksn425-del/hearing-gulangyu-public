# Context-aware AI Guide

## Why Context Matters

A generic AI tourist chatbot may answer with broad historical knowledge, but it may not know whether a specific visitor is on a steep route, near a risky pavement segment, or asking about a building from a particular location.

Hearing Gulangyu improves this by grounding AI interaction in spatial context.

## Context Inputs

The guide can be framed around:

- current node or location
- nearby heritage buildings
- destination
- route risk profile
- user type
- sensory notes
- map constraints
- recent user question or correction

## Prompt Logic

The AI guide should answer with three layers:

1. **Direct answer**: respond to the visitor's question.
2. **Site grounding**: connect the answer to Gulangyu map, point of interest, or route context.
3. **Accessibility note**: add slope, surface, crowd, or safety warning when relevant.

## Example Prompt Structure

```text
You are an accessible guide for Gulangyu Island.

User profile:
{user_type}

Current map context:
{current_node}
Nearby heritage points:
{nearby_pois}
Route risk:
{route_risk_summary}
Sensory notes:
{sensory_context}

Visitor question:
{user_question}

Answer with:
1. concise guide response
2. site-specific context
3. accessibility warning if needed
4. next action suggestion
```

## Failure Handling

When the AI is uncertain, the product should not guess. It should:

- ask a clarifying question
- suggest the nearest known point
- offer a safer route option
- tell the user when a photo or location is needed
- fall back to map-based route logic

## Product Interpretation

The AI value is not only language generation. The value is the integration of AI interaction, local map context, accessibility logic, and route-risk algorithms.
