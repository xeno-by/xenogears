﻿using System;
using System.Reflection;
using XenoGears.Assertions;

namespace XenoGears.Formats.Annotations.Engines
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public abstract class TypeEngineAttribute : EngineAttribute
    {
        public sealed override Object Deserialize(MemberInfo mi, Json json) { return Deserialize(mi.AssertCast<Type>(), json); }
        public sealed override Json Serialize(MemberInfo mi, Object value) { return Serialize(mi.AssertCast<Type>(), value); }

        public abstract Object Deserialize(Type t, Json json);
        public abstract Json Serialize(Type t, Object value);
    }
}