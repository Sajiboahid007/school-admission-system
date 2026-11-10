namespace AdmissionApi.Common;

// All "now" values go through one place (always UTC).
public interface IClock
{
    DateTime UtcNow { get; }
}

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
