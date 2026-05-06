using System.Collections.Generic;

namespace Verse
{
    public struct TaggedString
    {
        public string Value;
        public static implicit operator string(TaggedString ts) => ts.Value;
        public static implicit operator TaggedString(string s) => new TaggedString { Value = s };
        public override string ToString() => Value ?? "";
    }

    public interface IExposable
    {
        void ExposeData();
    }

    public static class Scribe_Values
    {
        public static void Look<T>(ref T value, string label, T? defaultValue = default!) { }
    }

    public static class Scribe_Collections
    {
        public static void Look<T>(ref List<T> list, string label, LookMode lookMode) { }
        public static void Look<T>(ref List<T> list, string label) { }
        public static void Look<TKey, TValue>(ref Dictionary<TKey, TValue> dict, string label, LookMode keyLookMode, LookMode valueLookMode) where TKey : notnull { }
    }

    public static class Scribe_Deep
    {
        public static void Look<T>(ref T target, string label) where T : IExposable, new() { }
    }

    public enum LookMode { Value, Deep }

    public static class Log
    {
        public static void Warning(string msg) { }
        public static void Message(string msg) { }
        public static void Error(string msg) { }
    }

    public static class Extensions
    {
        public static bool NullOrEmpty(this string? s) => string.IsNullOrEmpty(s);
    }
}

namespace RimWorld.Planet
{
    public class WorldComponent
    {
        public WorldComponent(World world) { }
        public virtual void ExposeData() { }
    }

    public class World { }
}
