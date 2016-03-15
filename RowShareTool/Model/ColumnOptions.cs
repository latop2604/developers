﻿using System;

namespace RowShareTool.Model
{
    [Flags]
    public enum ColumnOptions
    {

        None = 0,

        IsMandatory = 1,

        IsComputed = 2,

        IsHyperlink = 4,

        IsCurrency = 8,

        IsRichText = 16,

        IsLookupMultiValued = 32,

        IsUnique = 64,

        IsEmail = 128,

        IsFrequent = 256,
    }
}