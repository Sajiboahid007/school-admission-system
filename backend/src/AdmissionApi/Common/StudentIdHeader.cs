using System.Text.RegularExpressions;

namespace AdmissionApi.Common;

public static class StudentIdHeader
{
    public const string Name = "X-Student-Id";

    // [0-9] and NOT \d: in .NET, \d also matches Bangla digits (০-৯) and other scripts.
    private static readonly Regex Pattern = new("^[0-9]{17}$", RegexOptions.Compiled);

    public static string Require(HttpRequest request)
    {
        var value = request.Headers[Name].ToString().Trim();

        if (string.IsNullOrEmpty(value))
            throw new AppException(400, "STUDENT_ID_REQUIRED", "X-Student-Id header is required.");

        if (!Pattern.IsMatch(value))
            throw new AppException(400, "INVALID_STUDENT_ID", "Student id must be exactly 17 digits.");

        return value;
    }
}
