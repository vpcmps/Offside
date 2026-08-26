using Xunit;

// The error counter lives on a process-wide static Meter, so two test classes listening at the
// same time would see each other's measurements. Serialising the assembly is cheaper than
// threading a unique instrument through the recorder for tests alone.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
