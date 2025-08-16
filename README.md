# What Nata is
* _Nata_ is a functional toolkit written in F# for creating reliable, event-driven microservices.
* It focuses on explicit state transitions, finite state machines, and stream processing primitives to offer strong, practical guarantees when using optimistic concurrency.
* Nata separates pure domain logic from I/O and execution, which allows for thorough testing of domain code and easy swapping of infrastructure.

Core Ideas
* **Finite State Machines**: This concept involves modeling your domain using states and transitions that are triggered by inputs (events). This method promotes clarity, testability, and a clear understanding of what happens at each step.
* **Event Streams + Optimistic Concurrency**: Services in Nata keep track of their own state and their position within an input stream using a checkpoint. This allows them to be restarted at any time and resume from where they left off. Multiple instances can safely work on the same task, which supports high availability.
* **At-least-once Processing with Idempotence**: Handlers are invoked at least once for each event. Checkpoints and index journaling make it easier to build idempotent effects and achieve "exactly once" outcomes at the business level.

Main Capabilities and Their Importance

**1. Consumer Primitives (foundations for microservices)**
* `consume` / `consumeEvent`: Use these to subscribe to a stream and invoke a handler for each event, with checkpointing. This is the simplest way to reliably connect a side-effect to a stream and is ideal for sinks, observers, and integration points.
* `fold` / `foldEvent`: These primitives help maintain consistent, replayable state by folding events into an accumulator. They are core building blocks for aggregates, materialized views, and real-time computations, and their pure functions are easy to test and reason about.
* `map` / `mapEvent`: These are used to project input events to output events on a 1:1 basis. They are useful for transforming streams, for example, by enriching, translating, or normalizing data, while maintaining order and reliability.
* `bifold` / `bimap` (+ their Event variants): These allow you to merge two input streams of potentially different types into a single state or output stream. They are helpful for cleanly combining heterogeneous sources, such as revenue and expenses, while maintaining correct state and ordering.
* `multifold` / `multimap` (+ Event variants): Use these to fold or map across many inputs of the same type, with per-source indexing. This is useful for scaling to a large number of sources (e.g., devices, symbols) while retaining per-source positions to prevent skipping or double-counting.
* `partition` / `distribute` (+ Event variants): These fan out a single input stream to one or many outputs, with ordering guarantees per partition and controlled merging logic. They are used to build routing, sharding, and selective fan-out topologies while preserving order per partition and idempotence.
* `multipartition` / `multidistribute` (+ Event variants): These fan out with fan-in semantics, allowing multiple services to safely publish to the same output while tracking per-source indices. This enables the composition of complex topologies, such as many producers feeding a shared projection, without losing correctness.

**2. Binding Module (compose entire topologies)**
* Binding abstractions enable you to "snap" channels and domain functions together with strong type inference. Bindings are sequences that can be started either synchronously or asynchronously.
* You can seamlessly convert outputs back into inputs to create chained pipelines. This allows you to design the system's topology declaratively, letting the framework handle the execution model, supervision hooks, and type safety, while you focus on domain transformations and flow.

Design Benefits
* **Decoupling**: Nata keeps your domain logic pure and infrastructure-agnostic. You can swap channels, such as in-memory, files, or event stores, using adapters without changing the business code.
* **Reliability by construction**: The use of checkpointing with per-stream indices allows for safe restarts, blue/green deployments, and multi-instance competition without data loss.
* **Testability**: The logic for `fold` and `map` is pure and easy to unit test. The end-to-end correctness is achieved through small, composable functions.
* **Evolvability**: Since topologies are defined using bindings and small primitives, you can easily rewire flows and add new capabilities without rewriting core logic.
* **Observability hooks**: The execution model, with bindings as sequences, makes it easy to add supervision and monitoring.

Choosing the Right Primitive
* For side effects per event, start with `consume`/`consumeEvent`.
* For an aggregate or materialized state, use `fold`/`foldEvent`.
* To transform stream A into stream B, use `map`/`mapEvent`.
* To merge two sources into one state or output, use `bifold`/`bimap`.
* To scale across many like-typed sources, use `multifold`/`multimap`.
* For routing or sharding, use `partition`/`distribute`.
* For safe fan-in from multiple producers, use `multipartition`/`multidistribute`.

Practical Considerations
* **Non-determinism**: When using `bifold`/`bimap` and multi-source variants, events arrive at different times. The framework processes whichever arrives next without forcing symmetry, so your logic should be designed to be order-tolerant.
* **Idempotence**: Side effects must be idempotent or guarded by indices/checkpoints to prevent duplicates during retries.
* **Index heterogeneity**: When fanning in across different backends, it may be necessary to map indices to a normalized representation first.
* **Storage growth**: Fan-in (`multi-distribute`) tracks per-source indices in the outputs, so you should plan for the I/O and storage overhead.
* **Throughput vs. ordering**: The system preserves per-partition ordering where required. You should consider partitioning keys to balance the load and maintain the necessary ordering guarantees.

Example Problem Spaces
* **Trading and market data**: Use real-time folds for volumes and profit/loss, maps for signal pipelines, partitions by symbol, and fan-in for execution adapters.
* **IoT and smart devices**: Use multifolds across device streams, aggregations for alerts, and partitions for targeted processing.
* **Data integration and enrichment**: Map streams to normalized schemas, enrich them with external services, and selectively distribute them to downstream consumers.
* **Operational analytics**: Build materialized views with `fold`, route flows by priority with `partition`, and safely publish aggregated results from multiple producers.

Getting Started Step-by-Step
1. Model your domain as a finite state machine by listing your states and events.
2. Write pure functions, using `fold` to update state and `map` to transform events.
3. Choose channels or backends that support optimistic concurrency or provide reader/writer primitives.
4. Compose your topology using the `Binding` module to connect inputs to outputs with `fold`, `map`, `bifold`, etc..
5. Start the bindings synchronously or asynchronously, add monitoring, and iterate.
6. Write extensive unit tests for your pure functions and integration tests to verify channel semantics and checkpoints.

Bottom Line
Nata provides a principled and type-safe way to design event-driven systems where correctness is a core focus. By centering on finite state machines, pure functions, and optimistic concurrency, it allows small teams to achieve production-grade reliability and confidently evolve complex topologies.