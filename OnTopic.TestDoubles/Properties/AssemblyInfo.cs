﻿/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

/*==============================================================================================================================
| DEFINE ASSEMBLY ATTRIBUTES
>===============================================================================================================================
| Declare and define attributes used in the compiling of the finished assembly.
\-----------------------------------------------------------------------------------------------------------------------------*/
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: Guid("FE175884-59C1-4C4D-A663-4CC570432ECC")]

/*==============================================================================================================================
| HANDLE SUPPRESSIONS
>===============================================================================================================================
| Suppress warnings from code analysis that are either false positives or not relevant for this assembly.
\-----------------------------------------------------------------------------------------------------------------------------*/
[assembly: SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Expected by convention for OnTopic Editor", Scope = "namespaceanddescendants", Target = "~N:OnTopic.TestDoubles")]