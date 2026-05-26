using Artisan;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using YAPYAP;

namespace Artisan.Shared.Reflection
{
    public static class HarmonyUtil
{
    public static void CopyMatchingFieldsIncludingBaseTypes(object source, object target)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        Type sourceType = source.GetType();

        while (sourceType != null && sourceType != typeof(MonoBehaviour))
        {
            FieldInfo[] sourceFields = sourceType.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            foreach (FieldInfo sourceField in sourceFields)
            {
                if (sourceField.IsStatic || sourceField.IsInitOnly)
                    continue;

                FieldInfo targetField = FindFieldInHierarchy(target.GetType(), sourceField.Name);

                if (targetField == null)
                    continue;

                if (targetField.IsStatic || targetField.IsInitOnly)
                    continue;

                if (targetField.FieldType != sourceField.FieldType)
                    continue;

                targetField.SetValue(target, sourceField.GetValue(source));
            }

            sourceType = sourceType.BaseType;
        }
    }

    private static FieldInfo FindFieldInHierarchy(Type type, string fieldName)
    {
        while (type != null && type != typeof(MonoBehaviour))
        {
            FieldInfo field = type.GetField(
                fieldName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            if (field != null)
                return field;

            type = type.BaseType;
        }

        return null;
    }

    public static void CopySpellConfiguration(Spell source, Spell target)
    {
        HashSet<string> blacklist = new HashSet<string>
    {
        "spellData",

        "casterPawn",
        "NetworkcasterIdentity",

        "currentCharges",
        "isCasting",
        "isInitialized",
        "lastCastTime",
        "regenerationStartTime",

        "netIdentity",
        "syncObjects",
        "syncVarDirtyBits",

        "connectionToClient",
        "connectionToServer"
    };

        Type type = source.GetType();

        while (type != null && type != typeof(MonoBehaviour))
        {
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            foreach (FieldInfo field in fields)
            {
                if (field.IsStatic)
                    continue;

                if (blacklist.Contains(field.Name))
                    continue;

                FieldInfo targetField = AccessTools.Field(
                    target.GetType(),
                    field.Name
                );

                if (targetField == null)
                    continue;

                if (targetField.FieldType != field.FieldType)
                    continue;

                targetField.SetValue(
                    target,
                    field.GetValue(source)
                );
            }

            type = type.BaseType;
        }
    }

    public static void CopyMatchingFields(object source, object target)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        Type sourceType = source.GetType();
        Type targetType = target.GetType();

        FieldInfo[] sourceFields = sourceType.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        foreach (FieldInfo sourceField in sourceFields)
        {
            if (sourceField.IsStatic)
                continue;

            FieldInfo targetField = AccessTools.Field(targetType, sourceField.Name);

            if (targetField == null)
                continue;

            if (targetField.IsStatic || targetField.IsInitOnly)
                continue;

            if (targetField.FieldType != sourceField.FieldType)
                continue;

            object value = sourceField.GetValue(source);
            targetField.SetValue(target, value);

            ArtisanMod.Logger.LogInfo(
                $"Copied field '{sourceField.Name}' from '{sourceType.Name}' to '{targetType.Name}'."
            );
        }
    }

    public static void CopySerializedSpellFields(Spell source, Spell target)
    {
        System.Type type = source.GetType();

        while (type != null && type != typeof(Spell))
        {
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            foreach (FieldInfo field in fields)
            {
                if (field.IsStatic)
                    continue;

                if (field.IsInitOnly)
                    continue;

                if (AccessTools.Field(target.GetType(), field.Name) == null)
                    continue;

                field.SetValue(target, field.GetValue(source));
            }

            type = type.BaseType;
        }
    }

    public static void CopySerializedFields<T>(T source, T target)
            where T : ScriptableObject
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        Type type = typeof(T);

        while (type != null && type != typeof(ScriptableObject))
        {
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            foreach (FieldInfo field in fields)
            {
                if (field.IsStatic || field.IsInitOnly)
                    continue;

                if (field.IsNotSerialized)
                    continue;

                field.SetValue(target, field.GetValue(source));
            }

            type = type.BaseType;
        }
    }

    public static T GetFieldValue<T>(object instance, string fieldName)
    {
        if (instance == null)
            return default;

        FieldInfo field = AccessTools.Field(instance.GetType(), fieldName);

        if (field == null && instance.GetType().BaseType != null)
            field = AccessTools.Field(instance.GetType().BaseType, fieldName);

        if (field == null)
        {
            ArtisanMod.Logger.LogWarning($"Field '{fieldName}' was not found on '{instance.GetType().FullName}'.");
            return default;
        }

        object value = field.GetValue(instance);

        if (value is T typedValue)
            return typedValue;

        return default;
    }

    public static void SetFieldValue<T>(object instance, string fieldName, T value)
    {
        if (instance == null)
            return;

        FieldInfo field = AccessTools.Field(instance.GetType(), fieldName);

        if (field == null && instance.GetType().BaseType != null)
            field = AccessTools.Field(instance.GetType().BaseType, fieldName);

        if (field == null)
        {
            ArtisanMod.Logger.LogWarning($"Field '{fieldName}' was not found on '{instance.GetType().FullName}'.");
            return;
        }

        field.SetValue(instance, value);
    }
}
}
