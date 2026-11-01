# Architecture

## 1. Technical Architecture

- **Stack:** [main language, frameworks, libraries]
- **Database:** [database technology and key design decisions]
- **Deployment:** [how the product is published — web, desktop, mobile; hosting platform]
- **Platform:** [web / desktop / mobile / CLI / etc.]

## 2. Conceptual Architecture

### Layers

[Describe the conceptual layers of the application — e.g., Presentation, Business Logic, Data Access.]

### Fundamental Principles

These principles MUST be followed by ALL specs and implementations:

1. **Extreme Detail & Determinism:** All specs must be created, maintained, and evolved with extreme detail, total completeness, and absolute determinism. No decision may be left pending for implementation time. Every spec must be implementable 100% by autonomous AI. 

2. **KISS (Keep It Simple, Stupid):** All implementations must be as simple as possible. Avoid unnecessary abstractions, patterns, or complexity. The AI must be able to implement, maintain, and evolve the code easily.

3. **DRY (Don't Repeat Yourself):** Avoid duplication whenever possible, provided it does not conflict with KISS. Simplicity takes precedence over deduplication when they conflict.

4. **AI-First Documentation:** All spec documents (requirements.md, design.md, task.md) are written exclusively for AI engines (especially DeepSeek V4 Pro). Human readability is secondary. Precision, completeness, and determinism are paramount.
