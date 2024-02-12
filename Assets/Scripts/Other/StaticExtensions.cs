using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public static class ColorISH
{
    public static readonly Color32 Red = new(255, 66, 78, 255);
    public static readonly Color32 Green = new(78, 255, 163, 255);
    public static readonly Color32 Blue = new(78, 97, 255, 255);

    public static readonly Color32 Cyan = new(78, 242, 255, 255);
    public static readonly Color32 Magenta = new(255, 57, 204, 255);
    public static readonly Color32 Yellow = new(255, 217, 78, 255);

    public static readonly Color32 Gold = new(255, 214, 48, 255);
    public static readonly Color32 Silver = new(168, 169, 173, 255);
    public static readonly Color32 Bronze = new(187, 123, 61, 255);

    public static readonly Color LightGray = new(0.75f, 0.75f, 0.75f, 1);
    public static readonly Color Invisible = new(1, 1, 1, 0);
}

public static class ExpressionHelper
{
    private static readonly DataTable dt = new();
    private static readonly Dictionary<string, string> expressionCache = new();
    private static readonly Dictionary<string, object> resultCache = new();

    private static readonly (string old, string @new)[] tokens = new[]
    {
        ("&&", "AND"),
        ("||", "OR"),
        ("==", "=")
    };

    public static T Compute<T>(this string expression, params (string name, object value)[] arguments) =>
        (T)Convert.ChangeType(expression.Transform().GetResult(arguments), typeof(T));

    private static object GetResult(this string expression, params (string name, object value)[] arguments)
    {
        foreach (var (name, value) in arguments)
            expression = expression.Replace(name, value.ToString());

        if (resultCache.TryGetValue(expression, out var result))
            return result;

        return resultCache[expression] = dt.Compute(expression, string.Empty);
    }

    private static string Transform(this string expression)
    {
        if (expressionCache.TryGetValue(expression, out var result))
            return result;

        result = expression;
        foreach (var (old, @new) in tokens)
            result = result.Replace(old, @new);

        return expressionCache[expression] = result;
    }
}
