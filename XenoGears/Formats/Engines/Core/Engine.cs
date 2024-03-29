﻿using System;
using System.Diagnostics;
using System.Reflection;

namespace XenoGears.Formats.Engines.Core
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    [DebuggerNonUserCode]
    public abstract class Engine : Attribute
    {
        public abstract Object Deserialize(MemberInfo mi, Json json);
        public abstract Json Serialize(MemberInfo mi, Object value);
    }
}