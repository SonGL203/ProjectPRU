using System.Collections.Generic;

public static class CowboyRoster
{
    public class Entry
    {
        public int id;
        public int tier;
        public bool placed;
    }

    static readonly List<Entry> entries = new();
    static int nextId = 1;

    public static int Count => entries.Count;

    public static Entry Add(int tier)
    {
        var e = new Entry { id = nextId++, tier = tier };
        entries.Add(e);
        return e;
    }

    public static Entry TakeUnplaced()
    {
        foreach (var e in entries)
            if (!e.placed) { e.placed = true; return e; }
        return null;
    }

    public static void Remove(int id) => entries.RemoveAll(e => e.id == id);

    public static void Clear() => entries.Clear();
}
