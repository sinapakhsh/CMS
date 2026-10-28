namespace CMS.Infrastructure.Messaging;

// Bound worker timing so a stalled delivery cannot hold ownership indefinitely.
public sealed class OutboxOptions
{
    // Operators may disable delivery while still allowing durable event capture.
    public bool Enabled { get; set; } = true;
    // Empty queues are polled without busy waiting.
    public int PollSeconds { get; set; } = 5;
    // Crashed workers relinquish ownership after the lease expires.
    public int LeaseSeconds { get; set; } = 60;
    // A delivery deadline must be shorter than the ownership lease.
    public int DeliveryTimeoutSeconds { get; set; } = 30;
    // Repeated failures remain in a dead-letter state for operator review.
    public int MaxAttempts { get; set; } = 8;
}
