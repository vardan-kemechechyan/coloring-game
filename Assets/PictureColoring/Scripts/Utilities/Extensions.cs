using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public static class Extensions
{
    private static System.Random random = new System.Random();

    public static float AddPercent(this float value, int percentage) 
    {
        return value + ((value / 100) * percentage);
    }

    public static float SubtractPercent(this float value, int percentage)
    {
        return value - ((value / 100) * percentage);
    }

    public static int RandomExcept(int n, int[] x)
    {
        int result = random.Next(n - x.Length);

        for (int i = 0; i < x.Length; i++)
        {
            if (result < x[i])
                return result;
            result++;
        }
        return result;
    }

    public static void Clone<T>(this T source, T target)
    {
        var type = typeof(T);
        foreach (var sourceProperty in type.GetProperties())
        {
            var targetProperty = type.GetProperty(sourceProperty.Name);
            targetProperty.SetValue(target, sourceProperty.GetValue(source, null), null);
        }
        foreach (var sourceField in type.GetFields())
        {
            var targetField = type.GetField(sourceField.Name);
            targetField.SetValue(target, sourceField.GetValue(source));
        }
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static int Clamp(this int value, int range)
    {
        int a = 0;
        int b = value;

        while (b > 0)
        {
            a++;
            b--;

            if (a >= range - 1)
                a = 0;
        }

        return a;
    }

    public static Color ToColor(this string value)
    {
        if (ColorUtility.TryParseHtmlString(value, out Color color))
        {
            return color;
        }
        else 
        {
            return Color.clear;
        }
    }

    public static string ToHtmlColor(this Color value)
    {
        return $"#{ColorUtility.ToHtmlStringRGB(value)}";
    }

    public static void MailTo(string email, string subject, string body) =>
         Application.OpenURL($"mailto:{email}?subject={subject.EscapeURL()}&body={body.EscapeURL()}");

    static string EscapeURL(this string url) => UnityWebRequest.EscapeURL(url).Replace("+", "%20");
}
