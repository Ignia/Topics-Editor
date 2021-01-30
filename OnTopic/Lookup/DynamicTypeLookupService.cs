﻿/*==============================================================================================================================
| Author        Ignia, LLC
| Client        Ignia, LLC
| Project       Topics Library
\=============================================================================================================================*/
using System;
using System.Linq;

namespace OnTopic.Lookup {

  /*============================================================================================================================
  | CLASS: DYNAMIC TYPE LOOKUP SERVICE
  \---------------------------------------------------------------------------------------------------------------------------*/
  /// <summary>
  ///   The <see cref="DynamicTypeLookupService"/> will search all assemblies for <see cref="Type"/> instances that match a
  ///   predicate.
  /// </summary>
  public class DynamicTypeLookupService : StaticTypeLookupService {

    /*==========================================================================================================================
    | CONSTRUCTOR
    \-------------------------------------------------------------------------------------------------------------------------*/
    /// <summary>
    ///   Establishes a new instance of a <see cref="DynamicTypeLookupService"/> based on a <paramref name="predicate"/> and,
    ///   optionally, a default <see cref="Type"/> object to return if none is specified.
    /// </summary>
    /// <param name="predicate">The search condition to use to identify target classes.</param>
    public DynamicTypeLookupService(Func<Type, bool> predicate) : base() {

      /*------------------------------------------------------------------------------------------------------------------------
      | Find target classes
      \-----------------------------------------------------------------------------------------------------------------------*/
      var matchedTypes = AppDomain
        .CurrentDomain
        .GetAssemblies()
        .SelectMany(t => t.GetTypes())
        .Where(t => t.IsClass && predicate(t))
        .OrderBy(t => t.Namespace?.StartsWith("OnTopic", StringComparison.Ordinal))
        .ToList();

      /*------------------------------------------------------------------------------------------------------------------------
      | Populate collection
      \-----------------------------------------------------------------------------------------------------------------------*/
      foreach (var type in matchedTypes) {
        TryAdd(type);
      }

    }

  } //Class
} //Namespace