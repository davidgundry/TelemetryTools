using UnityEngine;
using System.Collections;

public struct KeyID {

    private readonly int? id;

    public int AsInt { get { return (int) id; } }
    public bool IsSet { get { return id != null; } }

    public KeyID(int id)
    {
        this.id = id;
    }

    public static bool operator >(KeyID a, KeyID b)
    {
        return a.AsInt > b.AsInt;
    }

    public static bool operator <(KeyID a, KeyID b)
    {
        return a.AsInt < b.AsInt;
    }

    public static bool operator <(KeyID a, int b)
    {
        return a.AsInt < b;
    }

    public static bool operator >(KeyID a, int b)
    {
        return a.AsInt > b;
    }

    public static bool operator ==(KeyID a, KeyID b)
    {
        return a.AsInt == b.AsInt;
    }

    public static bool operator !=(KeyID a, KeyID b)
    {
        return a.AsInt != b.AsInt;
    }
}
