# customs-notification-hub

A notification dispatch service for customs events. When a customs declaration changes
state or a sanctions/license check fires, multiple parties frequently need to be reached
on multiple channels with different urgency. This service centralises the dispatch,
retry, and escalation logic so individual back-office systems do not each re-implement
the same delivery plumbing.

## Domain

The hub reasons about three main aggregates:

- **NotificationRequest** - a single business event. Carries `EventType`
  (for example `customs.declaration.accepted`), `Severity`, the `EntityType` and
  `EntityId` of the underlying declaration, a free-form `Payload`, and the canonical
  `Subject` / `Body` used for channel rendering.
- **Subscription** - a standing rule that says "recipient X wants events matching
  pattern P on channel C at address A, with escalation delay D". Patterns are evaluated
  as simple globs (`customs.*`, `*.expiring`, or an exact match).
- **Delivery** - one attempt (or retry) to reach one subscription for one
  notification. Deliveries are append-only and form the compliance trail.

### Typical event types

- `customs.declaration.accepted`
- `customs.declaration.rejected`
- `customs.inspection.scheduled`
- `customs.duty.assessed`
- `customs.clearance.granted`
- `customs.document.expiring`
- `customs.sanctions.hit`

## Subscription model

A subscription binds a recipient to an event-type pattern and a channel. The same
recipient can have several subscriptions (for example, SMS for `Critical` severity,
Email for everything). Each subscription carries:

- `EventTypePattern` - glob string (`*` wildcard only).
- `Channel` - `Email`, `Sms`, `Webhook`, or `InApp`.
- `Address` - channel-specific destination (email address, phone, URL, user id).
- `QuietHours` - optional local-time window in which delivery is suppressed (for
  non-critical severities).
- `EscalationDelayMinutes` - how long a `Critical` delivery may stay `Pending` before
  the next subscription in the chain is tried.

## Delivery state machine

```
           +-----------+
           |  Pending  |
           +-----+-----+
                 |
                 v
           +-----------+      ok      +-------------+
           |  Sending  |------------->|  Delivered  |
           +-----+-----+              +-------------+
                 |
           fail  |
                 v
           +-----------+      retries left        +-----------+
           |   Failed  |------------------------->|  Pending  |
           +-----+-----+                          +-----------+
                 |
         retries exhausted
                 v
           +-------------+
           |  Abandoned  |
           +-------------+
```

A `Delivered` or `Abandoned` record is terminal and never mutates after that point.

## Retry policy

`RetryPolicy` uses exponential backoff with full jitter:

- base delay `B` (default 30s)
- multiplier `2^(attempt - 1)` capped at a ceiling (default 15m)
- jitter picks a uniform value in `[0, capped_delay]`
- max attempts default 5; after that the delivery is `Abandoned`

## Escalation

When a `Critical` notification has subscriptions with `EscalationDelayMinutes > 0`,
`EscalationEngine` treats them as an ordered chain. If the active delivery for step
`N` is still `Pending` (or newly `Failed` without retries left) after the configured
delay, step `N + 1` is activated. Steps with `EscalationDelayMinutes == 0` are
parallel fan-out (all fire immediately).

## Quiet hours

For `Info` and `Warning` severity, a delivery whose subscription has `QuietHours`
covering the current local time is deferred. `Critical` bypasses quiet hours
unconditionally - customs events with legal deadlines do not wait for office hours.

## API surface (NotificationHub.Api)

- `POST /notifications` - submit a `NotificationRequest`. Returns `202 Accepted` with
  the ids of the `Delivery` rows created.
- `GET /notifications/{id}/deliveries` - delivery rows for a notification.
- `POST /subscriptions` - register a subscription.
- `GET /subscriptions` - list all subscriptions.

A handful of sample subscriptions are seeded on startup so the service is immediately
exercisable.

## Build and run

Requires .NET 8 SDK.

```
dotnet restore
dotnet build
dotnet run --project src/NotificationHub.Api
dotnet test
```

The API listens on the default Kestrel port. All data is held in in-memory stores;
restarting the process clears subscriptions and deliveries.

## Project layout

- `src/NotificationHub` - domain model, dispatcher, channel senders, stores.
- `src/NotificationHub.Api` - minimal ASP.NET Core host exposing the HTTP surface.
- `tests/NotificationHub.Tests` - xUnit tests for the glob matcher, retry policy,
  and dispatcher orchestration.
