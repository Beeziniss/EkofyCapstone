namespace EkofyApp.Tests.Helpers;

public static class TestExtensions
{
    public static T With<T>(this T obj, Action<T> action) where T : class
    {
        action(obj);
        return obj;
    }
}