# AI Product Flow Diagram

This diagram explains Hearing Gulangyu as a context-aware AI guide product, not simply an app with a chatbot.

```mermaid
flowchart LR
    A["Visitor Need<br/>ask, navigate, understand, and avoid risk"] --> B["Input<br/>voice / text / photo<br/>current location<br/>destination<br/>user type"]
    B --> C["AI Interpretation<br/>speech correction<br/>intent recognition<br/>photo question understanding"]
    C --> D["Site Grounding<br/>Gulangyu map nodes<br/>heritage POIs<br/>sensory notes<br/>route-risk context"]
    D --> E["Algorithm Layer<br/>route weighting by distance, slope, friction, and accessibility profile"]
    E --> F["Output<br/>guide answer<br/>route suggestion<br/>turn instruction<br/>accessibility warning"]
    F --> G["Validation<br/>Unity digital twin simulation<br/>hazard exposure<br/>A/B route metrics"]
    G --> H["Feedback Loop<br/>adjust prompts<br/>adjust route weights<br/>improve user experience"]
    H --> C
```

## Product Manager Reading

- **User problem:** Heritage-site visitors need safer and more context-aware guidance than generic map routing.
- **AI role:** Interpret multimodal visitor questions and ground responses in site context.
- **Algorithm role:** Select safer routes using accessibility and risk weights.
- **Validation:** Unity simulation and evaluation figures test whether the route and warning logic improve experience.
